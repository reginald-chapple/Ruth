using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.CheckImages;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class DownloadDecryptedCheckImageQueryHandler(
    FinanceDbContext dbContext,
    ICheckImageVault checkImageVault
) : IQueryHandler<DownloadDecryptedCheckImageQuery, DecryptedCheckImageResponse>
{
    public async Task<Result<DecryptedCheckImageResponse>> HandleAsync(
        DownloadDecryptedCheckImageQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var txId = FinancialTransactionId.From(query.TransactionId);
        var imgId = FinancialTransactionImageId.From(query.ImageId);

        var image = await dbContext
            .FinancialTransactionImages.AsNoTracking()
            .FirstOrDefaultAsync(i => i.TransactionId == txId && i.Id == imgId, cancellationToken);
        if (image is null)
        {
            return FinanceErrors.CheckImageInvalid;
        }

        var decrypted = await checkImageVault.DownloadAndDecryptAsync(image, cancellationToken);
        return decrypted.IsFailure
            ? Result.Failure<DecryptedCheckImageResponse>(decrypted.Errors)
            : new DecryptedCheckImageResponse(
                image.Id.Value,
                image.TransactionId.Value,
                image.ImageSide,
                image.ContentType,
                decrypted.Value
            );
    }
}
