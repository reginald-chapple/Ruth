namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Read projection for a single financial transaction account split.</summary>
/// <param name="Id">Split detail identifier.</param>
/// <param name="AccountId">Financial account identifier.</param>
/// <param name="AccountName">Financial account display name.</param>
/// <param name="Amount">Allocated split amount.</param>
/// <param name="InvoiceId">Optional settled invoice identifier.</param>
/// <param name="IsTaxDeductible">Immutable tax-deductibility snapshot captured at transaction time.</param>
/// <param name="Summary">Optional split memo.</param>
public sealed record FinancialTransactionSplitResponse(
    Guid Id,
    Guid AccountId,
    string AccountName,
    decimal Amount,
    Guid? InvoiceId,
    bool IsTaxDeductible,
    string? Summary
);
