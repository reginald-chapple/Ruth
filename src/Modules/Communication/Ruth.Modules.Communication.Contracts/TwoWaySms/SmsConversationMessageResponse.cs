namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Single message entry within a two-way SMS conversation thread (CommunicationResponse).</summary>
/// <param name="Id">Unique message identifier.</param>
/// <param name="SystemPhoneNumberId">Organization E.164 phone number identifier.</param>
/// <param name="SystemPhoneNumber">Organization E.164 number string.</param>
/// <param name="ParticipantPhoneNumber">Constituent E.164 phone number.</param>
/// <param name="ParticipantPersonId">Optional matched CRM person identifier.</param>
/// <param name="ParticipantDisplayName">Display name or phone fallback.</param>
/// <param name="Direction">Inbound, staff reply, or automated pipeline reply.</param>
/// <param name="MessageBody">SMS text content.</param>
/// <param name="MatchedKeyword">Keyword matched by an SmsPipeline, if any.</param>
/// <param name="IsRead">Whether staff marked this message read.</param>
/// <param name="RepliedByPersonId">Optional staff person identifier for manual replies.</param>
/// <param name="OccurredAtUtc">Message timestamp (UTC).</param>
public sealed record SmsConversationMessageResponse(
    Guid Id,
    Guid SystemPhoneNumberId,
    string SystemPhoneNumber,
    string ParticipantPhoneNumber,
    Guid? ParticipantPersonId,
    string ParticipantDisplayName,
    SmsMessageDirection Direction,
    string MessageBody,
    string? MatchedKeyword,
    bool IsRead,
    Guid? RepliedByPersonId,
    DateTimeOffset OccurredAtUtc
);
