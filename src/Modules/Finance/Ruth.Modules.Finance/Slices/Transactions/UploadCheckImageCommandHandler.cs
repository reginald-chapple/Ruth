using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.CheckImages;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
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

        if (string.IsNullOrWhiteSpace(command.ImageBase64))
        {
            return FinanceErrors.CheckImageInvalid;
        }

        byte[] rawBytes;
        try
        {
            rawBytes = Convert.FromBase64String(command.ImageBase64.Trim());
        }
        catch (FormatException)
        {
            return FinanceErrors.CheckImageInvalid;
        }

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

        var contentType = string.IsNullOrWhiteSpace(command.ContentType)
            ? "image/png"
            : command.ContentType.Trim();
        var attached = tx.AttachEncryptedImage(
            imageId,
            command.ImageSide,
            encrypted.Value.StorageObjectKey,
            contentType,
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
}
