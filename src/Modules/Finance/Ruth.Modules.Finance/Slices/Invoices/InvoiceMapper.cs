using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Invoices;

namespace Ruth.Modules.Finance.Slices.Invoices;

internal static class InvoiceMapper
{
    public static InvoiceResponse ToResponse(
        Invoice invoice,
        IReadOnlyDictionary<FinancialAccountId, string> accountNames,
        DateOnly today
    )
    {
        IReadOnlyList<InvoiceLineItemResponse> lines =
        [
            .. invoice.LineItems.Select(li => new InvoiceLineItemResponse(
                li.Id.Value,
                li.AccountId.Value,
                accountNames.TryGetValue(li.AccountId, out var name) ? name : "Account",
                li.Description,
                li.Quantity,
                li.UnitAmount,
                li.LineTotal,
                li.IsTaxDeductible
            )),
        ];

        return new InvoiceResponse(
            invoice.Id.Value,
            invoice.InvoiceNumber,
            invoice.PersonId,
            invoice.HouseholdId,
            invoice.BranchId,
            invoice.Status,
            invoice.IsOverdue(today),
            invoice.IssueDate,
            invoice.DueDate,
            invoice.CurrencyCode,
            invoice.SubtotalAmount,
            invoice.PaidAmount,
            invoice.BalanceDue,
            invoice.Memo,
            lines,
            invoice.CreatedOnUtc
        );
    }
}
