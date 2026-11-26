namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Single delivery, bounce, open, click, or unsubscribe event reported by an email/SMS provider webhook.</summary>
/// <param name="EventType">Telemetry event kind.</param>
/// <param name="RecipientId">Optional explicit Ruth recipient identifier.</param>
/// <param name="ProviderMessageId">Optional provider message correlation ID.</param>
/// <param name="DestinationAddress">Optional recipient email or phone number.</param>
/// <param name="BounceKind">Optional bounce severity when <paramref name="EventType" /> is <see cref="CommunicationWebhookEventType.Bounced" />.</param>
/// <param name="ClickedUrl">Optional clicked URL when <paramref name="EventType" /> is <see cref="CommunicationWebhookEventType.Clicked" />.</param>
/// <param name="Reason">Optional bounce or diagnostic reason.</param>
/// <param name="OccurredAtUtc">Optional provider event timestamp (UTC).</param>
public sealed record CommunicationWebhookEventDto(
    CommunicationWebhookEventType EventType,
    Guid? RecipientId = null,
    string? ProviderMessageId = null,
    string? DestinationAddress = null,
    BounceKind BounceKind = BounceKind.Hard,
    string? ClickedUrl = null,
    string? Reason = null,
    DateTimeOffset? OccurredAtUtc = null
);
