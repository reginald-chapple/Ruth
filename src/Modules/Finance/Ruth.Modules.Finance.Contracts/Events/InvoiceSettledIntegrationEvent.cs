using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Finance.Contracts.Events;

/// <summary>Outbox integration event emitted when an invoice transitions to fully paid status.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when settled.</param>
/// <param name="InvoiceId">Settled invoice identifier.</param>
/// <param name="InvoiceNumber">Human-readable invoice number.</param>
/// <param name="PersonId">Billed constituent or organization Person identifier.</param>
/// <param name="TotalPaidAmount">Total settled amount.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
public sealed record InvoiceSettledIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid PersonId,
    decimal TotalPaidAmount,
    string CurrencyCode
) : IIntegrationEvent;
