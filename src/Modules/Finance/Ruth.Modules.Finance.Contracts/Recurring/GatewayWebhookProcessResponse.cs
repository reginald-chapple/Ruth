using Ruth.Modules.Finance.Contracts.Gateways;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Result returned after verifying and processing an inbound payment gateway webhook.</summary>
/// <param name="WebhookEventId">Verified external webhook event ID.</param>
/// <param name="EventKind">Normalized webhook event kind.</param>
/// <param name="WasDuplicate">True when this webhook event ID was already processed idempotently.</param>
/// <param name="RecordedTransactionId">Optional transaction ID created by settlement.</param>
/// <param name="AffectedScheduleId">Optional recurring schedule ID updated by the webhook.</param>
public sealed record GatewayWebhookProcessResponse(
    string WebhookEventId,
    GatewayWebhookEventKind EventKind,
    bool WasDuplicate,
    Guid? RecordedTransactionId,
    Guid? AffectedScheduleId
);
