using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Batches;

/// <summary>Command to explicitly reopen a Closed financial batch with an audit reason.</summary>
/// <param name="BatchId">Target batch identifier.</param>
/// <param name="ReopenedByPersonId">Optional Person identifier authorizing the reopen.</param>
/// <param name="AuditReason">Required audit reason recorded on the batch.</param>
public sealed record ReopenFinancialBatchCommand(
    Guid BatchId,
    Guid? ReopenedByPersonId,
    string AuditReason
) : ICommand<FinancialBatchResponse>;
