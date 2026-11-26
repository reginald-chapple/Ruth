namespace Ruth.Modules.Finance.Contracts.Batches;

/// <summary>Reconciliation state machine status for a financial settlement batch.</summary>
public enum FinancialBatchStatus
{
    /// <summary>Batch created and awaiting opening/verification.</summary>
    Pending = 0,

    /// <summary>Batch open for posting transactions.</summary>
    Open = 1,

    /// <summary>Batch reconciled and audit-locked.</summary>
    Closed = 2,
}
