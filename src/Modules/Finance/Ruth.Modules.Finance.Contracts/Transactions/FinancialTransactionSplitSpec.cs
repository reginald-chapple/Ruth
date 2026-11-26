namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Input specification for an account/invoice split line on a financial transaction.</summary>
/// <param name="AccountId">Target financial account identifier.</param>
/// <param name="Amount">Positive monetary amount allocated to this split.</param>
/// <param name="InvoiceId">Optional open invoice identifier settled by this split.</param>
/// <param name="Summary">Optional split memo.</param>
public sealed record FinancialTransactionSplitSpec(
    Guid AccountId,
    decimal Amount,
    Guid? InvoiceId = null,
    string? Summary = null
);
