using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Finance.Infrastructure.CheckImages;

internal sealed class AesGcmCheckImageVault(
    IStorageProvider storageProvider,
    IOptions<FinanceSecurityOptions> securityOptions
) : ICheckImageVault
{
    private const int MaxCheckImageBytes = 10 * 1024 * 1024;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public async Task<
        Result<(
            string StorageObjectKey,
            long ByteLength,
            string Sha256Hex,
            string NonceBase64,
            string TagBase64,
            string KeyId
        )>
    > EncryptAndUploadAsync(
        FinancialTransactionId transactionId,
        FinancialTransactionImageId imageId,
        CheckImageSide side,
        ReadOnlyMemory<byte> plaintextBytes,
        CancellationToken cancellationToken = default
    )
    {
        if (plaintextBytes.IsEmpty || plaintextBytes.Length > MaxCheckImageBytes)
        {
            return FinanceErrors.CheckImageInvalid;
        }

        var keyBytes = securityOptions.Value.GetCheckImageAes256KeyBytes();
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var tag = new byte[TagSizeBytes];
        var ciphertext = new byte[plaintextBytes.Length];
        var aad = BuildAssociatedData(transactionId, imageId, side);

        using (var aes = new AesGcm(keyBytes, TagSizeBytes))
        {
            aes.Encrypt(nonce, plaintextBytes.Span, ciphertext, tag, aad);
        }

        var sha256Hex = Convert.ToHexStringLower(SHA256.HashData(plaintextBytes.Span));
        var sideKey = side.ToString();
        var rawObjectKey =
            $"finance/checks/{transactionId.Value:D}/{imageId.Value:D}-{sideKey}.enc";
        var objectKey = TenantStorageKeyScope.ResolveKey(
            AmbientTenantContext.Instance,
            rawObjectKey
        );

        await using var stream = new MemoryStream(ciphertext, writable: false);
        var uploaded = await storageProvider.UploadAsync(
            objectKey,
            stream,
            "application/octet-stream",
            cancellationToken
        );
        var effectiveKey = TenantStorageKeyScope.ResolveKey(
            AmbientTenantContext.Instance,
            uploaded.ObjectKey
        );

        return (
            effectiveKey,
            plaintextBytes.Length,
            sha256Hex,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            securityOptions.Value.CheckImageKeyId
        );
    }

    public async Task<Result<byte[]>> DownloadAndDecryptAsync(
        FinancialTransactionImage metadata,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(metadata);

        await using var stream = await storageProvider.OpenReadAsync(
            metadata.StorageObjectKey,
            cancellationToken
        );
        if (stream is null)
        {
            return FinanceErrors.CheckImageInvalid;
        }

        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        var ciphertext = ms.ToArray();

        try
        {
            var nonce = Convert.FromBase64String(metadata.EncryptionNonceBase64);
            var tag = Convert.FromBase64String(metadata.EncryptionTagBase64);
            var plaintext = new byte[ciphertext.Length];
            var aad = BuildAssociatedData(metadata.TransactionId, metadata.Id, metadata.ImageSide);

            using var aes = new AesGcm(
                securityOptions.Value.GetCheckImageAes256KeyBytes(),
                TagSizeBytes
            );
            aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);

            var actualSha256 = Convert.ToHexStringLower(SHA256.HashData(plaintext));
            return !string.Equals(
                actualSha256,
                metadata.Sha256PlaintextHex,
                StringComparison.OrdinalIgnoreCase
            )
                ? FinanceErrors.CheckImageInvalid
                : plaintext;
        }
        catch (CryptographicException)
        {
            return FinanceErrors.CheckImageInvalid;
        }
        catch (FormatException)
        {
            return FinanceErrors.CheckImageInvalid;
        }
    }

    private static byte[] BuildAssociatedData(
        FinancialTransactionId txId,
        FinancialTransactionImageId imgId,
        CheckImageSide side
    )
    {
        var sideText = side.ToString();
        return Encoding.UTF8.GetBytes($"{txId.Value:D}:{imgId.Value:D}:{sideText}");
    }
}
