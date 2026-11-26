using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Batches;

/// <summary>Query to retrieve a single financial batch with reconciliation totals.</summary>
/// <param name="BatchId">Target batch identifier.</param>
public sealed record GetFinancialBatchByIdQuery(Guid BatchId) : IQuery<FinancialBatchResponse>;
