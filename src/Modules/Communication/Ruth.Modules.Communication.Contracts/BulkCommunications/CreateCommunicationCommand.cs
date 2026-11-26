using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Creates a new draft Communication with recipients resolved from explicit entries and/or a target group.</summary>
/// <param name="Title">Campaign title.</param>
/// <param name="Channel">Target communication channel.</param>
/// <param name="TopicKey">Preference topic key (defaults to general-announcements).</param>
/// <param name="IsBulk">Whether bulk unsubscribe rules apply.</param>
/// <param name="TemplateId">Optional template identifier used to seed content when fields are omitted.</param>
/// <param name="Subject">Email subject template.</param>
/// <param name="PreheaderText">Optional email preheader text.</param>
/// <param name="FromName">Optional sender display name.</param>
/// <param name="FromEmail">Optional sender email address.</param>
/// <param name="ReplyToEmail">Optional reply-to email address.</param>
/// <param name="SenderSystemPhoneNumberId">Optional outbound SMS phone number identifier.</param>
/// <param name="RazorLayoutKey">Optional compiled Razor layout key.</param>
/// <param name="HtmlBodyOverride">Optional HTML body override when structured sections are omitted.</param>
/// <param name="SmsBody">Optional SMS plain-text merge template.</param>
/// <param name="PushTitle">Optional push notification title template.</param>
/// <param name="PushBody">Optional push notification body template.</param>
/// <param name="PushOpenUrl">Optional push notification action URL.</param>
/// <param name="Sections">Optional structured email sections.</param>
/// <param name="TargetGroupId">Optional Group identifier whose active members are added as recipients.</param>
/// <param name="Recipients">Explicit recipient entries.</param>
/// <param name="ScheduledSendAtUtc">Optional future send timestamp (UTC).</param>
/// <param name="CreatedByPersonId">Optional author person identifier.</param>
public sealed record CreateCommunicationCommand(
    string Title,
    CommunicationChannel Channel,
    string TopicKey = "general-announcements",
    bool IsBulk = true,
    Guid? TemplateId = null,
    string? Subject = null,
    string? PreheaderText = null,
    string? FromName = null,
    string? FromEmail = null,
    string? ReplyToEmail = null,
    Guid? SenderSystemPhoneNumberId = null,
    string? RazorLayoutKey = "Communication.StandardEmail",
    string? HtmlBodyOverride = null,
    string? SmsBody = null,
    string? PushTitle = null,
    string? PushBody = null,
    string? PushOpenUrl = null,
    IReadOnlyList<EmailSectionDto>? Sections = null,
    Guid? TargetGroupId = null,
    IReadOnlyList<CommunicationRecipientInputDto>? Recipients = null,
    DateTimeOffset? ScheduledSendAtUtc = null,
    Guid? CreatedByPersonId = null
) : ICommand<CommunicationDetailResponse>;
