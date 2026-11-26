using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Invoices;

/// <summary>Child line item belonging to an <see cref="Invoice" />.</summary>
public sealed class InvoiceLineItem : Entity<InvoiceLineItemId>
{
    private InvoiceLineItem() { }

    internal InvoiceLineItem(
        InvoiceLineItemId id,
        InvoiceId invoiceId,
        FinancialAccountId accountId,
        string description,
        decimal quantity,
        decimal unitAmount,
        decimal lineTotal,
        bool isTaxDeductible
    )
        : base(id)
    {
        InvoiceId = invoiceId;
        AccountId = accountId;
        Description = description;
        Quantity = quantity;
        UnitAmount = unitAmount;
        LineTotal = lineTotal;
        IsTaxDeductible = isTaxDeductible;
    }

    public InvoiceId InvoiceId { get; private set; }
    public FinancialAccountId AccountId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public bool IsTaxDeductible { get; private set; }
}
