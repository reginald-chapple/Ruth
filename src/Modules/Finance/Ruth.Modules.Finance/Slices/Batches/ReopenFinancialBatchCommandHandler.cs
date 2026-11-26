using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Batches;

internal sealed class ReopenFinancialBatchCommandHandler(
    FinanceDbContext dbContext,
    BatchReconciliationReader reconciliationReader
) : ICommandHandler<ReopenFinancialBatchCommand, FinancialBatchResponse>
{
    public async Task<Result<FinancialBatchResponse>> HandleAsync(
        ReopenFinancialBatchCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var batchId = FinancialBatchId.From(command.BatchId);
        var batch = await dbContext.FinancialBatches.FirstOrDefaultAsync(
            b => b.Id == batchId,
            cancellationToken
        );
        if (batch is null)
        {
            return FinanceErrors.BatchNotFound;
        }

        var reopened = batch.Reopen(command.ReopenedByPersonId, command.AuditReason);
        if (reopened.IsFailure)
        {
            return Result.Failure<FinancialBatchResponse>(reopened.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var (actualTotal, actualCount) = await reconciliationReader.GetActualsAsync(
            batchId,
            cancellationToken
        );
        return FinancialBatchMapper.ToResponse(batch, actualTotal, actualCount);
    }
}
