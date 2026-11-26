namespace Ruth.Modules.Finance.Contracts.Batches;

/// <summary>Read projection representing a financial settlement batch and control reconciliation totals.</summary>
/// <param name="Id">Unique batch identifier.</param>
/// <param name="Name">Batch display name.</param>
/// <param name="Status">Current state machine status.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="ControlAmount">Expected control total amount.</param>
/// <param name="ActualAmount">Actual sum of non-voided transactions in this batch.</param>
/// <param name="VarianceAmount">Difference (ActualAmount minus ControlAmount).</param>
/// <param name="ControlItemCount">Expected item count (0 when count check is not enforced).</param>
/// <param name="ActualItemCount">Actual count of non-voided transactions in this batch.</param>
/// <param name="IsBalanced">True when variance is zero and count matches control count.</param>
/// <param name="BatchStartDateTimeUtc">Batch window start UTC timestamp.</param>
/// <param name="BatchEndDateTimeUtc">Optional batch window end UTC timestamp.</param>
/// <param name="AccountingSystemCode">Optional external GL export reference code.</param>
/// <param name="IsAutomated">True when automatically managed by gateway or scanner jobs.</param>
/// <param name="ClosedByPersonId">Optional Person identifier who reconciled and closed the batch.</param>
/// <param name="ClosedOnUtc">Optional UTC timestamp when closed.</param>
/// <param name="AuditLockNote">Audit history note for close/reopen actions.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record FinancialBatchResponse(
    Guid Id,
    string Name,
    FinancialBatchStatus Status,
    Guid? BranchId,
    string CurrencyCode,
    decimal ControlAmount,
    decimal ActualAmount,
    decimal VarianceAmount,
    int ControlItemCount,
    int ActualItemCount,
    bool IsBalanced,
    DateTimeOffset BatchStartDateTimeUtc,
    DateTimeOffset? BatchEndDateTimeUtc,
    string? AccountingSystemCode,
    bool IsAutomated,
    Guid? ClosedByPersonId,
    DateTimeOffset? ClosedOnUtc,
    string? AuditLockNote,
    DateTimeOffset CreatedOnUtc
);
