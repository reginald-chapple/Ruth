using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Batches;

internal sealed class CloseFinancialBatchCommandHandler(
    FinanceDbContext dbContext,
    BatchReconciliationReader reconciliationReader,
    IClock clock
) : ICommandHandler<CloseFinancialBatchCommand, FinancialBatchResponse>
{
    public async Task<Result<FinancialBatchResponse>> HandleAsync(
        CloseFinancialBatchCommand command,
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

        var (actualTotal, actualCount) = await reconciliationReader.GetActualsAsync(
            batchId,
            cancellationToken
        );
        var closed = batch.Close(
            actualTotal,
            actualCount,
            command.ClosedByPersonId,
            clock.UtcNow,
            command.AuditNote
        );
        if (closed.IsFailure)
        {
            return Result.Failure<FinancialBatchResponse>(closed.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return FinancialBatchMapper.ToResponse(batch, actualTotal, actualCount);
    }
}
