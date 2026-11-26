using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Batches;

/// <summary>Query to list financial batches with live reconciliation totals.</summary>
/// <param name="Status">Optional filter by batch status.</param>
/// <param name="BranchId">Optional filter by Branch/Site identifier.</param>
public sealed record GetFinancialBatchesQuery(
    FinancialBatchStatus? Status = null,
    Guid? BranchId = null
) : IQuery<IReadOnlyList<FinancialBatchResponse>>;
