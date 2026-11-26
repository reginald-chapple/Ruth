namespace Ruth.Modules.Finance.Contracts.Accounts;

/// <summary>Read projection representing a hierarchical financial account or fund.</summary>
/// <param name="Id">Unique account identifier.</param>
/// <param name="ParentAccountId">Optional parent account identifier.</param>
/// <param name="Name">Internal account name.</param>
/// <param name="PublicName">External public display name.</param>
/// <param name="Code">Unique normalized account code.</param>
/// <param name="Description">Optional description.</param>
/// <param name="AccountKind">Operational classification of the account.</param>
/// <param name="IsTaxDeductible">Whether receipts against this account are tax-deductible.</param>
/// <param name="DefaultCurrencyCode">Default ISO-4217 currency code.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="GlCode">Optional general-ledger account code.</param>
/// <param name="CostCenterCode">Optional cost-center code.</param>
/// <param name="StartDate">Optional opening date.</param>
/// <param name="EndDate">Optional closing date.</param>
/// <param name="DisplayOrder">Display order within parent.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="IsPublic">Whether the account is visible publicly.</param>
/// <param name="Attributes">Extensible JSONB attribute values.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
/// <param name="ModifiedOnUtc">Last modification UTC timestamp.</param>
public sealed record FinancialAccountResponse(
    Guid Id,
    Guid? ParentAccountId,
    string Name,
    string PublicName,
    string Code,
    string? Description,
    FinancialAccountKind AccountKind,
    bool IsTaxDeductible,
    string DefaultCurrencyCode,
    Guid? BranchId,
    string? GlCode,
    string? CostCenterCode,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int DisplayOrder,
    bool IsActive,
    bool IsPublic,
    IReadOnlyDictionary<string, string?> Attributes,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
