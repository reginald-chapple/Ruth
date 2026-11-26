using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain.Statements;

namespace Ruth.Modules.Finance.Slices.Statements;

internal static class StatementMapper
{
    public static FinancialStatementTemplateResponse ToResponse(FinancialStatementTemplate entity)
    {
        return new FinancialStatementTemplateResponse(
            entity.Id.Value,
            entity.Name,
            entity.StatementKind,
            entity.OrganizationDisplayName,
            entity.OrganizationTaxId,
            entity.CurrencyCode,
            entity.Description,
            entity.LogoUrl,
            entity.HeaderMergeTemplate,
            entity.FooterMergeTemplate,
            entity.CustomPrintCss,
            entity.IncludePledgeSummary,
            entity.IncludeOpenInvoices,
            entity.MinimumTotalThreshold,
            entity.IsDefault,
            entity.IsActive,
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc
        );
    }

    public static GeneratedFinancialStatementResponse ToResponse(
        GeneratedFinancialStatement entity,
        string templateName
    )
    {
        return new GeneratedFinancialStatementResponse(
            entity.Id.Value,
            entity.StatementNumber,
            entity.TemplateId.Value,
            templateName,
            entity.StatementKind,
            entity.BatchRunId,
            entity.RecipientScope,
            entity.TargetPersonId,
            entity.TargetHouseholdId,
            entity.RecipientDisplayName,
            entity.PeriodStartDate,
            entity.PeriodEndDate,
            entity.CurrencyCode,
            entity.TaxDeductibleTotal,
            entity.NonDeductiblePaidTotal,
            entity.OpenInvoiceBalanceTotal,
            entity.QualifyingTransactionCount,
            entity.HtmlStorageKey,
            entity.PdfStorageKey,
            entity.GeneratedAtUtc
        );
    }
}
