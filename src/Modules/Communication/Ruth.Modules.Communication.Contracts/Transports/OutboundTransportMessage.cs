namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Immutable outbound payload dispatched through an <see cref="ICommunicationTransport" />.</summary>
/// <param name="MessageId">Unique correlation identifier for the outbound message.</param>
/// <param name="Channel">Target communication medium channel.</param>
/// <param name="ToAddress">Destination email address, E.164 phone number, or device push token.</param>
/// <param name="ToDisplayName">Optional recipient display name.</param>
/// <param name="FromAddress">Optional sender email address or E.164 phone number.</param>
/// <param name="FromDisplayName">Optional sender display name.</param>
/// <param name="ReplyToAddress">Optional reply-to email address.</param>
/// <param name="Subject">Optional email subject line.</param>
/// <param name="HtmlBody">Optional pre-rendered HTML body for email messages.</param>
/// <param name="PlainTextBody">Optional plain-text body for email, SMS, or push notifications.</param>
/// <param name="PushTitle">Optional push notification heading.</param>
/// <param name="PushActionUrl">Optional deep-link or action URL for push notifications.</param>
/// <param name="SystemCommunicationKey">Optional originating system communication key.</param>
/// <param name="Metadata">Optional provider metadata tags.</param>
public sealed record OutboundTransportMessage(
    Guid MessageId,
    CommunicationChannel Channel,
    string ToAddress,
    string? ToDisplayName = null,
    string? FromAddress = null,
    string? FromDisplayName = null,
    string? ReplyToAddress = null,
    string? Subject = null,
    string? HtmlBody = null,
    string? PlainTextBody = null,
    string? PushTitle = null,
    string? PushActionUrl = null,
    string? SystemCommunicationKey = null,
    IReadOnlyDictionary<string, string>? Metadata = null
);
