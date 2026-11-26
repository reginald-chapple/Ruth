using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Command to create a draft or immediately issued invoice.</summary>
/// <param name="PersonId">Billed constituent or Business organization Person identifier.</param>
/// <param name="IssueDate">Invoice issue date.</param>
/// <param name="DueDate">Invoice due date (must be on or after IssueDate).</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="LineItems">One or more invoice line items.</param>
/// <param name="IssueImmediately">When true, transitions the invoice directly from Draft to Open.</param>
/// <param name="HouseholdId">Optional Household billing identifier.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="Memo">Optional memo or payment instructions.</param>
public sealed record CreateInvoiceCommand(
    Guid PersonId,
    DateOnly IssueDate,
    DateOnly DueDate,
    string CurrencyCode,
    IReadOnlyList<InvoiceLineItemSpec> LineItems,
    bool IssueImmediately = true,
    Guid? HouseholdId = null,
    Guid? BranchId = null,
    string? Memo = null
) : ICommand<InvoiceResponse>;
