namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Summary of an invoice issued or carrying an open balance during the statement period.</summary>
public sealed record StatementInvoiceSummaryDto(
    Guid InvoiceId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    string Status,
    decimal SubtotalAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    string CurrencyCode
);
