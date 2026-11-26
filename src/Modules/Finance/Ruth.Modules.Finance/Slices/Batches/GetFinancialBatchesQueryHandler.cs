using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Batches;

internal sealed class GetFinancialBatchesQueryHandler(
    FinanceDbContext dbContext,
    BatchReconciliationReader reconciliationReader
) : IQueryHandler<GetFinancialBatchesQuery, IReadOnlyList<FinancialBatchResponse>>
{
    public async Task<Result<IReadOnlyList<FinancialBatchResponse>>> HandleAsync(
        GetFinancialBatchesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var batchesQuery = dbContext.FinancialBatches.AsNoTracking().AsQueryable();
        if (query.Status is { } status)
        {
            batchesQuery = batchesQuery.Where(b => b.Status == status);
        }

        if (query.BranchId is { } branchId && branchId != Guid.Empty)
        {
            batchesQuery = batchesQuery.Where(b => b.BranchId == branchId);
        }

        var batches = await batchesQuery
            .OrderByDescending(static b => b.BatchStartDateTimeUtc)
            .ToListAsync(cancellationToken);

        if (batches.Count == 0)
        {
            IReadOnlyList<FinancialBatchResponse> empty = [];
            return Result.Success(empty);
        }

        var actuals = await reconciliationReader.GetActualsByBatchIdsAsync(
            [.. batches.Select(static b => b.Id)],
            cancellationToken
        );

        IReadOnlyList<FinancialBatchResponse> mapped =
        [
            .. batches.Select(b =>
            {
                _ = actuals.TryGetValue(b.Id, out var totals);
                return FinancialBatchMapper.ToResponse(b, totals.TotalAmount, totals.ItemCount);
            }),
        ];
        return Result.Success(mapped);
    }
}
