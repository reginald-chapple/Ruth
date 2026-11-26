namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Inbox conversation thread summary grouped by organization phone number and constituent phone number.</summary>
/// <param name="SystemPhoneNumberId">Organization system phone number identifier.</param>
/// <param name="SystemPhoneNumber">Organization E.164 phone number.</param>
/// <param name="ParticipantPhoneNumber">Constituent E.164 phone number.</param>
/// <param name="ParticipantPersonId">Optional matched CRM person identifier.</param>
/// <param name="ParticipantDisplayName">Constituent display name or phone number.</param>
/// <param name="LastMessagePreview">Snippet of the most recent message in the thread.</param>
/// <param name="LastMessageAtUtc">Timestamp of the most recent message (UTC).</param>
/// <param name="UnreadInboundCount">Count of unread inbound messages in this thread.</param>
/// <param name="TotalMessagesCount">Total messages in this thread.</param>
public sealed record SmsConversationSummaryResponse(
    Guid SystemPhoneNumberId,
    string SystemPhoneNumber,
    string ParticipantPhoneNumber,
    Guid? ParticipantPersonId,
    string ParticipantDisplayName,
    string LastMessagePreview,
    DateTimeOffset LastMessageAtUtc,
    int UnreadInboundCount,
    int TotalMessagesCount
);
