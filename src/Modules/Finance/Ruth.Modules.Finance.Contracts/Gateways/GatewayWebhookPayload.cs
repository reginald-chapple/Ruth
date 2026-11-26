namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Verified, normalized webhook payload parsed by an <see cref="IPaymentGateway" /> provider.</summary>
/// <param name="WebhookEventId">Unique external webhook delivery/event identifier for idempotency.</param>
/// <param name="EventKind">Normalized webhook event classification.</param>
/// <param name="GatewaySubscriptionReference">Optional external subscription identifier.</param>
/// <param name="GatewayTransactionId">Optional external transaction identifier.</param>
/// <param name="Amount">Optional monetary amount reported by the event.</param>
/// <param name="CurrencyCode">Optional ISO-4217 currency code.</param>
/// <param name="FailureMessage">Optional failure or decline reason.</param>
/// <param name="OccurredAtUtc">UTC timestamp reported by the gateway event.</param>
public sealed record GatewayWebhookPayload(
    string WebhookEventId,
    GatewayWebhookEventKind EventKind,
    string? GatewaySubscriptionReference,
    string? GatewayTransactionId,
    decimal? Amount,
    string? CurrencyCode,
    string? FailureMessage,
    DateTimeOffset OccurredAtUtc
);
