namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Projection of an individual recipient's delivery and telemetry state within a communication.</summary>
/// <param name="Id">Unique recipient record identifier.</param>
/// <param name="CommunicationId">Owning communication identifier.</param>
/// <param name="PersonId">Optional CRM person identifier.</param>
/// <param name="DestinationAddress">Normalized destination address (email, E.164 phone, or push token).</param>
/// <param name="DisplayName">Recipient display name.</param>
/// <param name="Status">Current delivery status.</param>
/// <param name="TransportKey">Transport key used during dispatch.</param>
/// <param name="ProviderMessageId">Downstream provider message identifier.</param>
/// <param name="BounceKind">Recorded bounce or complaint classification.</param>
/// <param name="OpenedCount">Total tracked open count.</param>
/// <param name="FirstOpenedAtUtc">First open timestamp (UTC).</param>
/// <param name="ClickedCount">Total tracked link click count.</param>
/// <param name="FirstClickedAtUtc">First link click timestamp (UTC).</param>
/// <param name="LastClickedUrl">Most recently clicked URL.</param>
/// <param name="SentAtUtc">Timestamp when the transport accepted the message (UTC).</param>
/// <param name="StatusNote">Optional delivery, skip, or bounce diagnostic note.</param>
public sealed record CommunicationRecipientResponse(
    Guid Id,
    Guid CommunicationId,
    Guid? PersonId,
    string DestinationAddress,
    string? DisplayName,
    RecipientDeliveryStatus Status,
    string? TransportKey,
    string? ProviderMessageId,
    BounceKind BounceKind,
    int OpenedCount,
    DateTimeOffset? FirstOpenedAtUtc,
    int ClickedCount,
    DateTimeOffset? FirstClickedAtUtc,
    string? LastClickedUrl,
    DateTimeOffset? SentAtUtc,
    string? StatusNote
);
