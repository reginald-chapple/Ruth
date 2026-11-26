using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Command to void an unpaid invoice.</summary>
/// <param name="InvoiceId">Target invoice identifier.</param>
/// <param name="Reason">Audit reason for voiding the invoice.</param>
public sealed record VoidInvoiceCommand(Guid InvoiceId, string Reason) : ICommand<InvoiceResponse>;
