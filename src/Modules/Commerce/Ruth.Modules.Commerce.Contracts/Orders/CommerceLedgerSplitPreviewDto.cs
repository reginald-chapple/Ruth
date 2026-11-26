namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Account-level ledger split preview showing exact routing between OperatingRevenue and CharitableFund accounts.</summary>
/// <param name="AccountId">Target Finance FinancialAccount identifier.</param>
/// <param name="IsTaxDeductibleCharitableSplit">True when this split represents tax-deductible charitable excess.</param>
/// <param name="Amount">Total monetary amount routed to this account.</param>
/// <param name="Summary">Descriptive memo for the ledger split.</param>
public sealed record CommerceLedgerSplitPreviewDto(
    Guid AccountId,
    bool IsTaxDeductibleCharitableSplit,
    decimal Amount,
    string Summary
);
