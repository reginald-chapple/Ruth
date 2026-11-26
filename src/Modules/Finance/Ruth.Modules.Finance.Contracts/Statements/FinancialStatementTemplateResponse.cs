namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Read projection for a configured financial statement or tax receipt template.</summary>
public sealed record FinancialStatementTemplateResponse(
    Guid Id,
    string Name,
    FinancialStatementKind StatementKind,
    string OrganizationDisplayName,
    string? OrganizationTaxId,
    string CurrencyCode,
    string? Description,
    string? LogoUrl,
    string? HeaderMergeTemplate,
    string? FooterMergeTemplate,
    string? CustomPrintCss,
    bool IncludePledgeSummary,
    bool IncludeOpenInvoices,
    decimal MinimumTotalThreshold,
    bool IsDefault,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
