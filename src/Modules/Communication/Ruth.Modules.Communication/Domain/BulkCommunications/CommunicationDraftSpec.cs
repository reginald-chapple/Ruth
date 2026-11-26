using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Specification for creating or updating a <see cref="Communication" /> campaign draft.</summary>
public sealed record CommunicationDraftSpec(
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
    DateTimeOffset? ScheduledSendAtUtc = null,
    Guid? CreatedByPersonId = null
);
