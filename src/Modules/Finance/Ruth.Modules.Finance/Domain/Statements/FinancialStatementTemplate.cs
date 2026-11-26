using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.Statements;

/// <summary>Configures print-CSS, merge headers/footers, and accounting scope for tax receipts and account statements.</summary>
public sealed class FinancialStatementTemplate
    : AggregateRoot<FinancialStatementTemplateId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private FinancialStatementTemplate() { }

    private FinancialStatementTemplate(FinancialStatementTemplateId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public FinancialStatementKind StatementKind { get; private set; }
    public string OrganizationDisplayName { get; private set; } = string.Empty;
    public string? OrganizationTaxId { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? HeaderMergeTemplate { get; private set; }
    public string? FooterMergeTemplate { get; private set; }
    public string? CustomPrintCss { get; private set; }
    public bool IncludePledgeSummary { get; private set; } = true;
    public bool IncludeOpenInvoices { get; private set; } = true;
    public decimal MinimumTotalThreshold { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a validated <see cref="FinancialStatementTemplate" /> aggregate.</summary>
    public static Result<FinancialStatementTemplate> Create(FinancialStatementTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var template = new FinancialStatementTemplate(FinancialStatementTemplateId.Create());
        var updated = template.Update(spec);
        return updated.IsFailure
            ? Result.Failure<FinancialStatementTemplate>(updated.Errors)
            : template;
    }

    /// <summary>Validates and updates mutable template fields.</summary>
    public Result Update(FinancialStatementTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var validation = ValidateSpec(spec, out var currency, out var scale);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = spec.Name.Trim();
        StatementKind = spec.StatementKind;
        OrganizationDisplayName = spec.OrganizationDisplayName.Trim();
        OrganizationTaxId = TrimOptional(spec.OrganizationTaxId, 64);
        CurrencyCode = currency;
        Description = TrimOptional(spec.Description, 500);
        LogoUrl = TrimOptional(spec.LogoUrl, 500);
        HeaderMergeTemplate = TrimOptional(spec.HeaderMergeTemplate, 4000);
        FooterMergeTemplate = TrimOptional(spec.FooterMergeTemplate, 4000);
        CustomPrintCss = TrimOptional(spec.CustomPrintCss, 4000);
        IncludePledgeSummary = spec.IncludePledgeSummary;
        IncludeOpenInvoices = spec.IncludeOpenInvoices;
        MinimumTotalThreshold = decimal.Round(
            spec.MinimumTotalThreshold,
            scale,
            MidpointRounding.AwayFromZero
        );
        IsDefault = spec.IsDefault;
        IsActive = spec.IsActive;

        return Result.Success();
    }

    /// <summary>Clears the default flag when another template of the same kind becomes default.</summary>
    public void ClearDefault()
    {
        IsDefault = false;
    }

    private static Result ValidateSpec(
        FinancialStatementTemplateSpec spec,
        out string currency,
        out int scale
    )
    {
        currency = "USD";
        scale = 2;

        return IsInvalidRequiredText(spec.Name, 160)
            || IsInvalidRequiredText(spec.OrganizationDisplayName, 160)
                ? FinanceErrors.StatementTemplateInvalid
            : !IsoCurrencyCatalog.TryGetScale(spec.CurrencyCode, out currency, out scale)
                ? FinanceErrors.CurrencyCodeInvalid
            : spec.MinimumTotalThreshold < 0m || !IsSafeLogoUrl(spec.LogoUrl)
                ? FinanceErrors.StatementTemplateInvalid
            : IsSafePrintCss(spec.CustomPrintCss) ? Result.Success()
            : FinanceErrors.StatementTemplateUnsafeCss;
    }

    private static bool IsInvalidRequiredText(string? value, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength;
    }

    private static bool IsSafeLogoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        var trimmed = url.Trim();
        return trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith('/');
    }

    private static bool IsSafePrintCss(string? css)
    {
        return string.IsNullOrWhiteSpace(css)
            || (
                !css.Contains("</style", StringComparison.OrdinalIgnoreCase)
                && !css.Contains("<script", StringComparison.OrdinalIgnoreCase)
                && !css.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
                && !css.Contains("@import", StringComparison.OrdinalIgnoreCase)
                && !css.Contains("expression(", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
