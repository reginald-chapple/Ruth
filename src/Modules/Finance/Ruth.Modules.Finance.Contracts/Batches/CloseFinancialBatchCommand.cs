using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Batches;

/// <summary>Command to reconcile and audit-lock an Open financial batch into Closed status.</summary>
/// <param name="BatchId">Target batch identifier.</param>
/// <param name="ClosedByPersonId">Optional Person identifier closing the batch.</param>
/// <param name="AuditNote">Optional closing reconciliation note.</param>
public sealed record CloseFinancialBatchCommand(
    Guid BatchId,
    Guid? ClosedByPersonId = null,
    string? AuditNote = null
) : ICommand<FinancialBatchResponse>;
