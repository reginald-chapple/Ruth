namespace Ruth.Modules.Finance.Contracts.Accounts;

/// <summary>Specification payload for creating or updating a financial account.</summary>
/// <param name="Name">Internal staff display name of the account.</param>
/// <param name="PublicName">External donor/member-facing name of the account.</param>
/// <param name="Code">Unique normalized account code (for example, GEN-FUND or MEM-DUES).</param>
/// <param name="AccountKind">Operational classification of the account.</param>
/// <param name="IsTaxDeductible">Whether contributions to this account qualify as tax-deductible receipts.</param>
/// <param name="DefaultCurrencyCode">Default 3-letter ISO-4217 currency code.</param>
/// <param name="ParentAccountId">Optional parent financial account ID for hierarchy rollup.</param>
/// <param name="BranchId">Optional operating Branch/Site ID associated with this account.</param>
/// <param name="GlCode">Optional general-ledger accounting code.</param>
/// <param name="CostCenterCode">Optional cost-center or department code.</param>
/// <param name="Description">Optional account description.</param>
/// <param name="StartDate">Optional inclusive opening date for accepting postings.</param>
/// <param name="EndDate">Optional inclusive closing date for accepting postings.</param>
/// <param name="DisplayOrder">Sort order among sibling accounts.</param>
/// <param name="IsActive">Whether the account is active for postings.</param>
/// <param name="IsPublic">Whether the account is selectable in public portals.</param>
/// <param name="Attributes">Optional extensible key/value attributes.</param>
public sealed record FinancialAccountSpec(
    string Name,
    string? PublicName,
    string Code,
    FinancialAccountKind AccountKind,
    bool IsTaxDeductible,
    string DefaultCurrencyCode = "USD",
    Guid? ParentAccountId = null,
    Guid? BranchId = null,
    string? GlCode = null,
    string? CostCenterCode = null,
    string? Description = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int DisplayOrder = 0,
    bool IsActive = true,
    bool IsPublic = true,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
