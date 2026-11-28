using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.CheckImages;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class UploadCheckImageCommandHandler(
    FinanceDbContext dbContext,
    ICheckImageVault checkImageVault,
    IClock clock
) : ICommandHandler<UploadCheckImageCommand, FinancialTransactionImageResponse>
{
    public async Task<Result<FinancialTransactionImageResponse>> HandleAsync(
        UploadCheckImageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var decoded = DecodeAndValidateCheckImage(command.ImageBase64, command.ContentType);
        if (decoded.IsFailure)
        {
            return Result.Failure<FinancialTransactionImageResponse>(decoded.Errors);
        }

        var (rawBytes, canonicalMime) = decoded.Value;
        var txId = FinancialTransactionId.From(command.TransactionId);
        var tx = await dbContext
            .FinancialTransactions.Include(static t => t.Images)
            .FirstOrDefaultAsync(t => t.Id == txId, cancellationToken);
        if (tx is null)
        {
            return FinanceErrors.TransactionNotFound;
        }

        var imageId = FinancialTransactionImageId.Create();
        var encrypted = await checkImageVault.EncryptAndUploadAsync(
            txId,
            imageId,
            command.ImageSide,
            rawBytes,
            cancellationToken
        );
        if (encrypted.IsFailure)
        {
            return Result.Failure<FinancialTransactionImageResponse>(encrypted.Errors);
        }

        var attached = tx.AttachEncryptedImage(
            imageId,
            command.ImageSide,
            encrypted.Value.StorageObjectKey,
            canonicalMime,
            encrypted.Value.ByteLength,
            encrypted.Value.Sha256Hex,
            encrypted.Value.NonceBase64,
            encrypted.Value.TagBase64,
            encrypted.Value.KeyId,
            clock.UtcNow
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return FinancialTransactionMapper.ToImageResponse(attached);
    }

    private static Result<(byte[] RawBytes, string CanonicalMime)> DecodeAndValidateCheckImage(
        string? imageBase64,
        string? contentType
    )
    {
        if (string.IsNullOrWhiteSpace(imageBase64))
        {
            return FinanceErrors.CheckImageInvalid;
        }

        byte[] rawBytes;
        try
        {
            rawBytes = Convert.FromBase64String(imageBase64.Trim());
        }
        catch (FormatException)
        {
            return FinanceErrors.CheckImageInvalid;
        }

        var validation = FileContentSecurityInspector.ValidateCheckImage(rawBytes, contentType);
        return validation.IsValid
            ? (rawBytes, validation.CanonicalMimeType)
            : FinanceErrors.CheckImageInvalid;
    }
}
