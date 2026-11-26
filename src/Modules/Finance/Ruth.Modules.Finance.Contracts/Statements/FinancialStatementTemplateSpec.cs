namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Specification for creating or updating a financial statement or tax receipt template.</summary>
/// <param name="Name">Internal administrative template name.</param>
/// <param name="StatementKind">Whether the template renders tax-deductible receipts, non-deductible statements, or separated combined statements.</param>
/// <param name="OrganizationDisplayName">Legal organization name printed on statements and tax receipts.</param>
/// <param name="OrganizationTaxId">Optional EIN, charity registration number, or tax identifier.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="Description">Optional administrative description.</param>
/// <param name="LogoUrl">Optional HTTPS or root-relative organization logo URL.</param>
/// <param name="HeaderMergeTemplate">Optional safe merge-token header markup.</param>
/// <param name="FooterMergeTemplate">Optional safe merge-token compliance or tax-disclosure footer markup.</param>
/// <param name="CustomPrintCss">Optional print stylesheet overrides (validated to exclude scripts or external imports).</param>
/// <param name="IncludePledgeSummary">Whether active pledge commitments should be summarized on the statement.</param>
/// <param name="IncludeOpenInvoices">Whether open and partially paid invoices should be listed in the non-deductible section.</param>
/// <param name="MinimumTotalThreshold">Minimum activity threshold required during batch statement runs.</param>
/// <param name="IsDefault">Whether this template is the default for its <see cref="StatementKind" />.</param>
/// <param name="IsActive">Whether this template is active for generation.</param>
public sealed record FinancialStatementTemplateSpec(
    string Name,
    FinancialStatementKind StatementKind,
    string OrganizationDisplayName,
    string? OrganizationTaxId = null,
    string CurrencyCode = "USD",
    string? Description = null,
    string? LogoUrl = null,
    string? HeaderMergeTemplate = null,
    string? FooterMergeTemplate = null,
    string? CustomPrintCss = null,
    bool IncludePledgeSummary = true,
    bool IncludeOpenInvoices = true,
    decimal MinimumTotalThreshold = 0m,
    bool IsDefault = false,
    bool IsActive = true
);
