using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Batches;

internal sealed class GetFinancialBatchByIdQueryHandler(
    FinanceDbContext dbContext,
    BatchReconciliationReader reconciliationReader
) : IQueryHandler<GetFinancialBatchByIdQuery, FinancialBatchResponse>
{
    public async Task<Result<FinancialBatchResponse>> HandleAsync(
        GetFinancialBatchByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var batchId = FinancialBatchId.From(query.BatchId);
        var batch = await dbContext
            .FinancialBatches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
        {
            return FinanceErrors.BatchNotFound;
        }

        var (actualTotal, actualCount) = await reconciliationReader.GetActualsAsync(
            batchId,
            cancellationToken
        );
        return FinancialBatchMapper.ToResponse(batch, actualTotal, actualCount);
    }
}
