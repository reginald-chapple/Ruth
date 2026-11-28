using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// FIPS-197 AES-256-GCM + RFC 5869 HKDF-SHA256 per-tenant AAD-bound envelope encryption implementation.
/// </summary>
public sealed class AesGcmTenantCredentialProtector : ITenantCredentialProtector
{
    /// <summary>Canonical version prefix for serialized AES-256-GCM credential envelopes.</summary>
    public const string EnvelopePrefix = "$ruth-env-v1$";

    private readonly FrozenDictionary<string, byte[]> _kekRing;

    /// <summary>Initializes a new instance of the <see cref="AesGcmTenantCredentialProtector" /> class.</summary>
    public AesGcmTenantCredentialProtector(TenantEnvelopeEncryptionOptions? options = null)
    {
        var effective = options ?? new TenantEnvelopeEncryptionOptions();
        ActiveKeyId = NormalizeKeyId(effective.ActiveKeyId);
        _kekRing = BuildKekRing(effective, ActiveKeyId);
    }

    /// <inheritdoc />
    public string ActiveKeyId { get; }

    /// <inheritdoc />
    public bool IsProtectedEnvelope(string? candidate)
    {
        return !string.IsNullOrWhiteSpace(candidate)
            && candidate.StartsWith(EnvelopePrefix, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public string Protect(TenantId tenantId, string purpose, string plaintext)
    {
        EnsureTenantContext(tenantId);
        ArgumentNullException.ThrowIfNull(plaintext);
        var normalizedPurpose = NormalizePurpose(purpose);

        var dek = RandomNumberGenerator.GetBytes(TenantEnvelopeCryptoCodec.DekSizeBytes);
        var wrappingKey = TenantEnvelopeCryptoCodec.DeriveWrappingKey(
            _kekRing[ActiveKeyId],
            tenantId,
            ActiveKeyId,
            normalizedPurpose
        );
        try
        {
            return TenantEnvelopeCryptoCodec.SealEnvelope(
                wrappingKey,
                dek,
                plaintext,
                tenantId,
                ActiveKeyId,
                normalizedPurpose
            );
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
            CryptographicOperations.ZeroMemory(wrappingKey);
        }
    }

    /// <inheritdoc />
    public string Unprotect(TenantId tenantId, string purpose, string envelopeCiphertext)
    {
        EnsureTenantContext(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelopeCiphertext);
        var normalizedPurpose = NormalizePurpose(purpose);

        var (keyId, wrappedSegment, dataSegment) = TenantEnvelopeCryptoCodec.ParseEnvelope(
            envelopeCiphertext
        );
        if (!_kekRing.TryGetValue(keyId, out var kekMaterial))
        {
            throw new CryptographicException(
                $"Unknown envelope encryption key identifier '{keyId}'."
            );
        }

        var wrappingKey = TenantEnvelopeCryptoCodec.DeriveWrappingKey(
            kekMaterial,
            tenantId,
            keyId,
            normalizedPurpose
        );
        var dek = new byte[TenantEnvelopeCryptoCodec.DekSizeBytes];
        try
        {
            TenantEnvelopeCryptoCodec.UnwrapDek(
                wrappingKey,
                wrappedSegment,
                dek,
                tenantId,
                keyId,
                normalizedPurpose
            );
            return TenantEnvelopeCryptoCodec.DecryptPayload(
                dek,
                dataSegment,
                tenantId,
                keyId,
                normalizedPurpose
            );
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
            CryptographicOperations.ZeroMemory(wrappingKey);
        }
    }

    /// <inheritdoc />
    public bool TryUnprotect(
        TenantId tenantId,
        string purpose,
        string? envelopeCiphertext,
        out string plaintext
    )
    {
        plaintext = string.Empty;
        if (
            tenantId.IsEmpty
            || string.IsNullOrWhiteSpace(purpose)
            || !IsProtectedEnvelope(envelopeCiphertext)
        )
        {
            return false;
        }

        try
        {
            plaintext = Unprotect(tenantId, purpose, envelopeCiphertext!);
            return true;
        }
        catch (CryptographicException)
        {
            plaintext = string.Empty;
            return false;
        }
    }

    private static FrozenDictionary<string, byte[]> BuildKekRing(
        TenantEnvelopeEncryptionOptions options,
        string activeKeyId
    )
    {
        var master = string.IsNullOrWhiteSpace(options.MasterKey)
            ? TenantEnvelopeEncryptionOptions.DevelopmentDefaultMasterKey
            : options.MasterKey.Trim();
        var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [activeKeyId] = SHA256.HashData(Encoding.UTF8.GetBytes(master)),
        };

        foreach (var pair in options.PreviousKeys)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            {
                map[NormalizeKeyId(pair.Key)] = SHA256.HashData(
                    Encoding.UTF8.GetBytes(pair.Value.Trim())
                );
            }
        }

        return map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static void EnsureTenantContext(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new TenantContextRequiredException(
                "Per-tenant credential encryption requires a non-empty TenantId."
            );
        }
    }

    private static string NormalizeKeyId(string? keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new ArgumentException("ActiveKeyId is required.", nameof(keyId));
        }

        var trimmed = keyId.Trim();
        return trimmed.Contains('$', StringComparison.Ordinal)
            ? throw new ArgumentException("KeyId cannot contain '$'.", nameof(keyId))
            : trimmed;
    }

    private static string NormalizePurpose(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        return purpose.Trim().ToLowerInvariant();
    }
}
