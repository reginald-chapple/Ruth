namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Read projection representing an invoice and its payment balance state.</summary>
/// <param name="Id">Unique invoice identifier.</param>
/// <param name="InvoiceNumber">Unique human-readable invoice number.</param>
/// <param name="PersonId">Billed constituent or Business organization Person identifier.</param>
/// <param name="HouseholdId">Optional Household billing identifier.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="IsOverdue">True when open or partially paid past DueDate.</param>
/// <param name="IssueDate">Invoice issue date.</param>
/// <param name="DueDate">Invoice payment due date.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="SubtotalAmount">Sum of line item totals.</param>
/// <param name="PaidAmount">Settled payments applied to this invoice.</param>
/// <param name="BalanceDue">Remaining unpaid balance.</param>
/// <param name="Memo">Optional billing memo or notes.</param>
/// <param name="LineItems">Ordered invoice line items.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record InvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    Guid PersonId,
    Guid? HouseholdId,
    Guid? BranchId,
    InvoiceStatus Status,
    bool IsOverdue,
    DateOnly IssueDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal SubtotalAmount,
    decimal PaidAmount,
    decimal BalanceDue,
    string? Memo,
    IReadOnlyList<InvoiceLineItemResponse> LineItems,
    DateTimeOffset CreatedOnUtc
);
