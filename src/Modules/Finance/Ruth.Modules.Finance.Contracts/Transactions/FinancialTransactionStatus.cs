namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Settlement lifecycle status of a financial transaction.</summary>
public enum FinancialTransactionStatus
{
    /// <summary>Cleared and settled.</summary>
    Settled = 0,

    /// <summary>Authorized or initiated awaiting settlement.</summary>
    Pending = 1,

    /// <summary>Partially refunded.</summary>
    PartiallyRefunded = 2,

    /// <summary>Fully refunded.</summary>
    Refunded = 3,

    /// <summary>Voided prior to settlement.</summary>
    Voided = 4,
}
