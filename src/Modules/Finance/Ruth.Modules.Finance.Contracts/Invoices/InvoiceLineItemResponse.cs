namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Read projection for a single invoice line item.</summary>
/// <param name="Id">Line item identifier.</param>
/// <param name="AccountId">Financial account identifier.</param>
/// <param name="AccountName">Financial account display name.</param>
/// <param name="Description">Line item description.</param>
/// <param name="Quantity">Item quantity.</param>
/// <param name="UnitAmount">Unit monetary price.</param>
/// <param name="LineTotal">Calculated line item total.</param>
/// <param name="IsTaxDeductible">Snapshotted tax deductibility of the target account.</param>
public sealed record InvoiceLineItemResponse(
    Guid Id,
    Guid AccountId,
    string AccountName,
    string Description,
    decimal Quantity,
    decimal UnitAmount,
    decimal LineTotal,
    bool IsTaxDeductible
);
