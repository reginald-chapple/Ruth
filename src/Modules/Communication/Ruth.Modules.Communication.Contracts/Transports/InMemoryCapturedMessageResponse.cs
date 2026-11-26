namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Message captured by the development in-memory sink or push stub transport.</summary>
/// <param name="MessageId">Unique message identifier.</param>
/// <param name="TransportKey">Transport key that captured the message.</param>
/// <param name="Channel">Channel medium.</param>
/// <param name="ToAddress">Recipient email, phone number, or push token.</param>
/// <param name="FromAddress">Optional sender email or phone number.</param>
/// <param name="Subject">Optional email subject.</param>
/// <param name="HtmlBody">Optional rendered HTML body.</param>
/// <param name="PlainTextBody">Optional plain-text body.</param>
/// <param name="PushTitle">Optional push notification title.</param>
/// <param name="SystemCommunicationKey">Optional originating system communication key.</param>
/// <param name="ProviderMessageId">Assigned provider message identifier.</param>
/// <param name="CapturedOnUtc">UTC timestamp when captured.</param>
public sealed record InMemoryCapturedMessageResponse(
    Guid MessageId,
    string TransportKey,
    CommunicationChannel Channel,
    string ToAddress,
    string? FromAddress,
    string? Subject,
    string? HtmlBody,
    string? PlainTextBody,
    string? PushTitle,
    string? SystemCommunicationKey,
    string ProviderMessageId,
    DateTimeOffset CapturedOnUtc
);
