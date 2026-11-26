namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Metadata summary of a generated tax receipt or account statement artifact.</summary>
public sealed record GeneratedFinancialStatementResponse(
    Guid Id,
    string StatementNumber,
    Guid TemplateId,
    string TemplateName,
    FinancialStatementKind StatementKind,
    Guid? BatchRunId,
    FinancialStatementRecipientScope RecipientScope,
    Guid? TargetPersonId,
    Guid? TargetHouseholdId,
    string RecipientDisplayName,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    string CurrencyCode,
    decimal TaxDeductibleTotal,
    decimal NonDeductiblePaidTotal,
    decimal OpenInvoiceBalanceTotal,
    int QualifyingTransactionCount,
    string? HtmlStorageKey,
    string? PdfStorageKey,
    DateTimeOffset GeneratedAtUtc
);
