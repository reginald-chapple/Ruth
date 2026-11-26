using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Command to transition a Draft invoice to Open for payment collection.</summary>
/// <param name="InvoiceId">Target invoice identifier.</param>
public sealed record IssueInvoiceCommand(Guid InvoiceId) : ICommand<InvoiceResponse>;
