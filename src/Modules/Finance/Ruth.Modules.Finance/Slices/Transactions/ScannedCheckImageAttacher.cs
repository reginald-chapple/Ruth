using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure.CheckImages;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class ScannedCheckImageAttacher(ICheckImageVault checkImageVault, IClock clock)
{
    public async Task<Result> AttachOptionalImagesAsync(
        FinancialTransaction tx,
        string? frontImageBase64,
        string? backImageBase64,
        string contentType,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var front = await AttachSingleAsync(
            tx,
            CheckImageSide.Front,
            frontImageBase64,
            contentType,
            nowUtc,
            cancellationToken
        );
        return front.IsFailure
            ? front
            : await AttachSingleAsync(
                tx,
                CheckImageSide.Back,
                backImageBase64,
                contentType,
                nowUtc,
                cancellationToken
            );
    }

    private async Task<Result> AttachSingleAsync(
        FinancialTransaction tx,
        CheckImageSide side,
        string? base64,
        string contentType,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return Result.Success();
        }

        byte[] raw;
        try
        {
            raw = Convert.FromBase64String(base64.Trim());
        }
        catch (FormatException)
        {
            return FinanceErrors.CheckImageInvalid;
        }

        var validation = FileContentSecurityInspector.ValidateCheckImage(raw, contentType);
        if (!validation.IsValid)
        {
            return FinanceErrors.CheckImageInvalid;
        }

        var imgId = FinancialTransactionImageId.Create();
        var stored = await checkImageVault.EncryptAndUploadAsync(
            tx.Id,
            imgId,
            side,
            raw,
            cancellationToken
        );
        if (stored.IsFailure)
        {
            return Result.Failure(stored.Errors);
        }

        _ = tx.AttachEncryptedImage(
            imgId,
            side,
            stored.Value.StorageObjectKey,
            validation.CanonicalMimeType,
            stored.Value.ByteLength,
            stored.Value.Sha256Hex,
            stored.Value.NonceBase64,
            stored.Value.TagBase64,
            stored.Value.KeyId,
            nowUtc
        );
        return Result.Success();
    }
}
