using System.Globalization;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.Statements;

/// <summary>Persisted audit and retrieval record for a generated constituent, household, or organization statement/receipt.</summary>
public sealed class GeneratedFinancialStatement
    : AggregateRoot<GeneratedFinancialStatementId>,
        IAuditableEntity
{
    private GeneratedFinancialStatement() { }

    private GeneratedFinancialStatement(
        GeneratedFinancialStatementId id,
        string statementNumber,
        FinancialStatementTemplateId templateId,
        FinancialStatementKind statementKind,
        Guid? batchRunId,
        FinancialStatementRecipientScope recipientScope,
        Guid? targetPersonId,
        Guid? targetHouseholdId,
        string recipientDisplayName,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        string currencyCode,
        decimal taxDeductibleTotal,
        decimal nonDeductiblePaidTotal,
        decimal openInvoiceBalanceTotal,
        int qualifyingTransactionCount,
        DateTimeOffset generatedAtUtc
    )
        : base(id)
    {
        StatementNumber = statementNumber;
        TemplateId = templateId;
        StatementKind = statementKind;
        BatchRunId = batchRunId;
        RecipientScope = recipientScope;
        TargetPersonId = targetPersonId;
        TargetHouseholdId = targetHouseholdId;
        RecipientDisplayName = recipientDisplayName;
        PeriodStartDate = periodStartDate;
        PeriodEndDate = periodEndDate;
        CurrencyCode = currencyCode;
        TaxDeductibleTotal = taxDeductibleTotal;
        NonDeductiblePaidTotal = nonDeductiblePaidTotal;
        OpenInvoiceBalanceTotal = openInvoiceBalanceTotal;
        QualifyingTransactionCount = qualifyingTransactionCount;
        GeneratedAtUtc = generatedAtUtc.ToUniversalTime();
    }

    public string StatementNumber { get; private set; } = string.Empty;
    public FinancialStatementTemplateId TemplateId { get; private set; }
    public FinancialStatementKind StatementKind { get; private set; }
    public Guid? BatchRunId { get; private set; }
    public FinancialStatementRecipientScope RecipientScope { get; private set; }
    public Guid? TargetPersonId { get; private set; }
    public Guid? TargetHouseholdId { get; private set; }
    public string RecipientDisplayName { get; private set; } = string.Empty;
    public DateOnly PeriodStartDate { get; private set; }
    public DateOnly PeriodEndDate { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal TaxDeductibleTotal { get; private set; }
    public decimal NonDeductiblePaidTotal { get; private set; }
    public decimal OpenInvoiceBalanceTotal { get; private set; }
    public int QualifyingTransactionCount { get; private set; }
    public string? HtmlStorageKey { get; private set; }
    public string? PdfStorageKey { get; private set; }
    public DateTimeOffset GeneratedAtUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <summary>Creates a validated <see cref="GeneratedFinancialStatement" /> record.</summary>
    public static Result<GeneratedFinancialStatement> Create(
        FinancialStatementTemplateId templateId,
        FinancialStatementKind statementKind,
        FinancialStatementRecipientScope recipientScope,
        Guid? targetPersonId,
        Guid? targetHouseholdId,
        string recipientDisplayName,
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        string currencyCode,
        decimal taxDeductibleTotal,
        decimal nonDeductiblePaidTotal,
        decimal openInvoiceBalanceTotal,
        int qualifyingTransactionCount,
        DateTimeOffset generatedAtUtc,
        Guid? batchRunId = null
    )
    {
        if (templateId.IsEmpty || string.IsNullOrWhiteSpace(recipientDisplayName))
        {
            return FinanceErrors.StatementRecipientInvalid;
        }

        if (periodEndDate < periodStartDate)
        {
            return FinanceErrors.StatementPeriodInvalid;
        }

        var normalizedPerson = NormalizeGuid(targetPersonId);
        var normalizedHousehold = NormalizeGuid(targetHouseholdId);
        if (!HasRequiredTarget(recipientScope, normalizedPerson, normalizedHousehold))
        {
            return FinanceErrors.StatementRecipientInvalid;
        }

        var id = GeneratedFinancialStatementId.Create();
        var prefix = statementKind == FinancialStatementKind.TaxDeductibleReceipt ? "RCPT" : "STMT";
        var suffix = id.Value.ToString("N")[^8..].ToUpperInvariant();
        var statementNumber = string.Create(
            CultureInfo.InvariantCulture,
            $"{prefix}-{periodEndDate:yyyy}-{suffix}"
        );

        return new GeneratedFinancialStatement(
            id,
            statementNumber,
            templateId,
            statementKind,
            NormalizeGuid(batchRunId),
            recipientScope,
            normalizedPerson,
            normalizedHousehold,
            recipientDisplayName.Trim(),
            periodStartDate,
            periodEndDate,
            currencyCode,
            taxDeductibleTotal,
            nonDeductiblePaidTotal,
            openInvoiceBalanceTotal,
            qualifyingTransactionCount,
            generatedAtUtc
        );
    }

    /// <summary>Records object storage keys for persisted HTML and PDF statement archives.</summary>
    public void AttachStorageKeys(string? htmlStorageKey, string? pdfStorageKey)
    {
        HtmlStorageKey = string.IsNullOrWhiteSpace(htmlStorageKey) ? null : htmlStorageKey.Trim();
        PdfStorageKey = string.IsNullOrWhiteSpace(pdfStorageKey) ? null : pdfStorageKey.Trim();
    }

    /// <summary>Repoints <see cref="TargetPersonId" /> when duplicate person records are merged.</summary>
    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (TargetPersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        TargetPersonId = survivingPersonId;
        return true;
    }

    private static bool HasRequiredTarget(
        FinancialStatementRecipientScope scope,
        Guid? personId,
        Guid? householdId
    )
    {
        return scope == FinancialStatementRecipientScope.Household
            ? householdId.HasValue
            : personId.HasValue;
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } guid && guid != Guid.Empty ? guid : null;
    }
}
