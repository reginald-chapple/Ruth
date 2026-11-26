namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Input specification for an invoice line item.</summary>
/// <param name="AccountId">Target revenue or fund financial account identifier.</param>
/// <param name="Description">Line item description.</param>
/// <param name="Quantity">Positive item quantity.</param>
/// <param name="UnitAmount">Non-negative unit price.</param>
public sealed record InvoiceLineItemSpec(
    Guid AccountId,
    string Description,
    decimal Quantity,
    decimal UnitAmount
);
