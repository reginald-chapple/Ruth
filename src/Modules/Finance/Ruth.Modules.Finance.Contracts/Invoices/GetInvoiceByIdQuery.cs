using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Query to fetch an invoice and its line items by identifier.</summary>
/// <param name="InvoiceId">Target invoice identifier.</param>
public sealed record GetInvoiceByIdQuery(Guid InvoiceId) : IQuery<InvoiceResponse>;
