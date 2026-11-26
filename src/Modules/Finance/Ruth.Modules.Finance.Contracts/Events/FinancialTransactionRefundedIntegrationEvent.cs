using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Finance.Contracts.Events;

/// <summary>Outbox integration event emitted when a full or partial refund is processed on a financial transaction.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when refunded.</param>
/// <param name="TransactionId">Original financial transaction identifier.</param>
/// <param name="RefundId">Created refund record identifier.</param>
/// <param name="AuthorizedPersonId">Optional constituent or organization Person identifier.</param>
/// <param name="RefundAmount">Refunded monetary amount.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="Reason">Refund reason.</param>
public sealed record FinancialTransactionRefundedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid TransactionId,
    Guid RefundId,
    Guid? AuthorizedPersonId,
    decimal RefundAmount,
    string CurrencyCode,
    string Reason
) : IIntegrationEvent;
