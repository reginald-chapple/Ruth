namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Lifecycle status of an invoice for membership dues, program fees, rentals, or sponsorships.</summary>
public enum InvoiceStatus
{
    /// <summary>Draft invoice not yet issued for payment.</summary>
    Draft = 0,

    /// <summary>Issued invoice awaiting payment.</summary>
    Open = 1,

    /// <summary>Issued invoice with partial payment applied.</summary>
    PartiallyPaid = 2,

    /// <summary>Invoice paid in full.</summary>
    Paid = 3,

    /// <summary>Voided invoice with zero active balance.</summary>
    Voided = 4,
}
