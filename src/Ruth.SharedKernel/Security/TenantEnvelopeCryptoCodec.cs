using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.SharedKernel.Security;

/// <summary>Low-level AES-256-GCM DEK wrap/unwrap, HKDF derivation, and envelope serialization codec.</summary>
internal static class TenantEnvelopeCryptoCodec
{
    public const int DekSizeBytes = 32;

    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const int WrappedDekSegmentBytes = NonceSizeBytes + TagSizeBytes + DekSizeBytes;
    private const int MinimumDataSegmentBytes = NonceSizeBytes + TagSizeBytes;

    public static string SealEnvelope(
        ReadOnlySpan<byte> wrappingKey,
        ReadOnlySpan<byte> dek,
        string plaintext,
        TenantId tenantId,
        string keyId,
        string purpose
    )
    {
        var wrappedSegment = WrapDek(wrappingKey, dek, tenantId, keyId, purpose);
        var dataSegment = EncryptPayload(dek, plaintext, tenantId, keyId, purpose);
        return $"{AesGcmTenantCredentialProtector.EnvelopePrefix}{keyId}${Base64Url.EncodeToString(wrappedSegment)}${Base64Url.EncodeToString(dataSegment)}";
    }

    public static void UnwrapDek(
        ReadOnlySpan<byte> wrappingKey,
        ReadOnlySpan<byte> wrappedSegment,
        Span<byte> dekDestination,
        TenantId tenantId,
        string keyId,
        string purpose
    )
    {
        var nonce = wrappedSegment[..NonceSizeBytes];
        var tag = wrappedSegment.Slice(NonceSizeBytes, TagSizeBytes);
        var ciphertext = wrappedSegment.Slice(NonceSizeBytes + TagSizeBytes, DekSizeBytes);
        var aad = BuildAssociatedData("dek-aad", tenantId, keyId, purpose);

        using var aes = new AesGcm(wrappingKey, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, dekDestination, aad);
    }

    public static string DecryptPayload(
        ReadOnlySpan<byte> dek,
        ReadOnlySpan<byte> dataSegment,
        TenantId tenantId,
        string keyId,
        string purpose
    )
    {
        var nonce = dataSegment[..NonceSizeBytes];
        var tag = dataSegment.Slice(NonceSizeBytes, TagSizeBytes);
        var ciphertext = dataSegment[MinimumDataSegmentBytes..];
        var plaintextBytes = new byte[ciphertext.Length];
        try
        {
            var aad = BuildAssociatedData("data-aad", tenantId, keyId, purpose);
            using var aes = new AesGcm(dek, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintextBytes, aad);
            return Encoding.UTF8.GetString(plaintextBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    public static (string KeyId, byte[] WrappedDek, byte[] DataSegment) ParseEnvelope(string raw)
    {
        if (
            !raw.StartsWith(
                AesGcmTenantCredentialProtector.EnvelopePrefix,
                StringComparison.Ordinal
            )
        )
        {
            throw new CryptographicException("Value is not a valid Ruth v1 credential envelope.");
        }

        var parts = raw[AesGcmTenantCredentialProtector.EnvelopePrefix.Length..].Split('$');
        if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[0]))
        {
            throw new CryptographicException("Malformed credential envelope segment count.");
        }

        byte[] wrapped;
        byte[] data;
        try
        {
            wrapped = Base64Url.DecodeFromChars(parts[1]);
            data = Base64Url.DecodeFromChars(parts[2]);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException(
                "Invalid Base64Url encoding in credential envelope.",
                ex
            );
        }

        return wrapped.Length != WrappedDekSegmentBytes || data.Length < MinimumDataSegmentBytes
            ? throw new CryptographicException("Credential envelope payload lengths are invalid.")
            : (parts[0], wrapped, data);
    }

    public static byte[] DeriveWrappingKey(
        byte[] kekMaterial,
        TenantId tenantId,
        string keyId,
        string purpose
    )
    {
        Span<byte> salt = stackalloc byte[16];
        _ = tenantId.Value.TryWriteBytes(salt);
        var info = Encoding.UTF8.GetBytes($"ruth:kek-wrap:v1|{keyId}|{purpose}");
        var output = new byte[DekSizeBytes];
        HKDF.DeriveKey(HashAlgorithmName.SHA256, kekMaterial, output, salt, info);
        return output;
    }

    private static byte[] WrapDek(
        ReadOnlySpan<byte> wrappingKey,
        ReadOnlySpan<byte> dek,
        TenantId tenantId,
        string keyId,
        string purpose
    )
    {
        var output = new byte[WrappedDekSegmentBytes];
        var nonce = output.AsSpan(0, NonceSizeBytes);
        var tag = output.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = output.AsSpan(NonceSizeBytes + TagSizeBytes, DekSizeBytes);

        RandomNumberGenerator.Fill(nonce);
        var aad = BuildAssociatedData("dek-aad", tenantId, keyId, purpose);
        using var aes = new AesGcm(wrappingKey, TagSizeBytes);
        aes.Encrypt(nonce, dek, ciphertext, tag, aad);
        return output;
    }

    private static byte[] EncryptPayload(
        ReadOnlySpan<byte> dek,
        string plaintext,
        TenantId tenantId,
        string keyId,
        string purpose
    )
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        try
        {
            var output = new byte[MinimumDataSegmentBytes + plaintextBytes.Length];
            var nonce = output.AsSpan(0, NonceSizeBytes);
            var tag = output.AsSpan(NonceSizeBytes, TagSizeBytes);
            var ciphertext = output.AsSpan(MinimumDataSegmentBytes, plaintextBytes.Length);

            RandomNumberGenerator.Fill(nonce);
            var aad = BuildAssociatedData("data-aad", tenantId, keyId, purpose);
            using var aes = new AesGcm(dek, TagSizeBytes);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, aad);
            return output;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    private static byte[] BuildAssociatedData(
        string stage,
        TenantId tenantId,
        string keyId,
        string purpose
    )
    {
        return Encoding.UTF8.GetBytes(
            $"ruth:{stage}:v1|tenant:{tenantId.Value:D}|key:{keyId}|purpose:{purpose}"
        );
    }
}
