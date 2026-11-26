using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Updates content, schedule, and recipient list of an editable draft or rejected Communication.</summary>
/// <param name="CommunicationId">Target communication identifier.</param>
/// <param name="Title">Updated campaign title.</param>
/// <param name="TopicKey">Updated preference topic key.</param>
/// <param name="IsBulk">Whether bulk unsubscribe rules apply.</param>
/// <param name="Subject">Updated email subject template.</param>
/// <param name="PreheaderText">Updated email preheader text.</param>
/// <param name="FromName">Optional sender display name.</param>
/// <param name="FromEmail">Optional sender email address.</param>
/// <param name="ReplyToEmail">Optional reply-to email address.</param>
/// <param name="SenderSystemPhoneNumberId">Optional outbound SMS phone number identifier.</param>
/// <param name="RazorLayoutKey">Optional compiled Razor layout key.</param>
/// <param name="HtmlBodyOverride">Optional HTML body override.</param>
/// <param name="SmsBody">Optional SMS body template.</param>
/// <param name="PushTitle">Optional push notification title template.</param>
/// <param name="PushBody">Optional push notification body template.</param>
/// <param name="PushOpenUrl">Optional push notification action URL.</param>
/// <param name="Sections">Updated structured email sections.</param>
/// <param name="Recipients">Optional replacement recipient list (when non-null).</param>
/// <param name="ScheduledSendAtUtc">Optional scheduled send timestamp (UTC).</param>
public sealed record UpdateCommunicationDraftCommand(
    Guid CommunicationId,
    string Title,
    string TopicKey = "general-announcements",
    bool IsBulk = true,
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
    IReadOnlyList<CommunicationRecipientInputDto>? Recipients = null,
    DateTimeOffset? ScheduledSendAtUtc = null
) : ICommand<CommunicationDetailResponse>;
