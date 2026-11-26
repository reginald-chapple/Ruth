using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Full detail projection of a Communication including composer sections and recipients.</summary>
/// <param name="Id">Unique communication identifier.</param>
/// <param name="Title">Campaign title.</param>
/// <param name="Channel">Communication channel.</param>
/// <param name="Status">Current lifecycle state.</param>
/// <param name="TopicKey">Subscription topic key.</param>
/// <param name="IsBulk">Whether bulk opt-out preferences apply.</param>
/// <param name="TemplateId">Optional originating template identifier.</param>
/// <param name="Subject">Optional email subject template.</param>
/// <param name="PreheaderText">Optional email preheader text.</param>
/// <param name="FromName">Optional sender display name.</param>
/// <param name="FromEmail">Optional sender email address.</param>
/// <param name="ReplyToEmail">Optional reply-to email address.</param>
/// <param name="SenderSystemPhoneNumberId">Optional outbound SMS phone number identifier.</param>
/// <param name="RazorLayoutKey">Optional compiled Razor layout key.</param>
/// <param name="HtmlBodyOverride">Optional custom HTML body override when sections are not used.</param>
/// <param name="SmsBody">Optional SMS body template.</param>
/// <param name="PushTitle">Optional push notification title template.</param>
/// <param name="PushBody">Optional push notification body template.</param>
/// <param name="PushOpenUrl">Optional push notification deep-link URL.</param>
/// <param name="Sections">Ordered structured email composer sections.</param>
/// <param name="CreatedByPersonId">Optional author person identifier.</param>
/// <param name="ReviewerPersonId">Optional reviewer person identifier.</param>
/// <param name="ApprovalNote">Optional approval or rejection comment.</param>
/// <param name="ScheduledSendAtUtc">Optional scheduled send window (UTC).</param>
/// <param name="SentAtUtc">Timestamp when batch dispatch finished (UTC).</param>
/// <param name="Recipients">Recipient delivery records.</param>
/// <param name="CreatedOnUtc">Creation timestamp (UTC).</param>
/// <param name="ModifiedOnUtc">Last modification timestamp (UTC).</param>
public sealed record CommunicationDetailResponse(
    Guid Id,
    string Title,
    CommunicationChannel Channel,
    CommunicationStatus Status,
    string TopicKey,
    bool IsBulk,
    Guid? TemplateId,
    string? Subject,
    string? PreheaderText,
    string? FromName,
    string? FromEmail,
    string? ReplyToEmail,
    Guid? SenderSystemPhoneNumberId,
    string? RazorLayoutKey,
    string? HtmlBodyOverride,
    string? SmsBody,
    string? PushTitle,
    string? PushBody,
    string? PushOpenUrl,
    IReadOnlyList<EmailSectionDto> Sections,
    Guid? CreatedByPersonId,
    Guid? ReviewerPersonId,
    string? ApprovalNote,
    DateTimeOffset? ScheduledSendAtUtc,
    DateTimeOffset? SentAtUtc,
    IReadOnlyList<CommunicationRecipientResponse> Recipients,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
