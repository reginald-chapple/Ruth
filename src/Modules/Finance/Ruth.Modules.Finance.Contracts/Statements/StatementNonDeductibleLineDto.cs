namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Line item representing a net settled non-deductible membership due, program fee, or invoice payment.</summary>
public sealed record StatementNonDeductibleLineDto(
    Guid TransactionId,
    DateTimeOffset TransactionDateUtc,
    string AccountName,
    string AccountCode,
    Guid? InvoiceId,
    string? Summary,
    decimal NetPaidAmount,
    string CurrencyCode
);
