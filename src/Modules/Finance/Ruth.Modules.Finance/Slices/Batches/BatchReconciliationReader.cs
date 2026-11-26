using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Infrastructure;

namespace Ruth.Modules.Finance.Slices.Batches;

internal sealed class BatchReconciliationReader(FinanceDbContext dbContext)
{
    public async Task<(decimal TotalAmount, int ItemCount)> GetActualsAsync(
        FinancialBatchId batchId,
        CancellationToken cancellationToken
    )
    {
        var rows = await dbContext
            .FinancialTransactions.AsNoTracking()
            .Where(t => t.BatchId == batchId && t.Status != FinancialTransactionStatus.Voided)
            .Select(static t => t.TotalAmount)
            .ToListAsync(cancellationToken);

        return (rows.Sum(), rows.Count);
    }

    public async Task<
        IReadOnlyDictionary<FinancialBatchId, (decimal TotalAmount, int ItemCount)>
    > GetActualsByBatchIdsAsync(
        IReadOnlyCollection<FinancialBatchId> batchIds,
        CancellationToken cancellationToken
    )
    {
        if (batchIds.Count == 0)
        {
            return new Dictionary<FinancialBatchId, (decimal, int)>();
        }

        var rows = await dbContext
            .FinancialTransactions.AsNoTracking()
            .Where(t =>
                t.BatchId.HasValue
                && batchIds.Contains(t.BatchId.Value)
                && t.Status != FinancialTransactionStatus.Voided
            )
            .Select(static t => new { BatchId = t.BatchId!.Value, t.TotalAmount })
            .ToListAsync(cancellationToken);

        return rows.GroupBy(static r => r.BatchId)
            .ToDictionary(
                static g => g.Key,
                static g => (g.Sum(static x => x.TotalAmount), g.Count())
            );
    }
}
