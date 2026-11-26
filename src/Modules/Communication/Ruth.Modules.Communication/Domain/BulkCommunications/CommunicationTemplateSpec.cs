using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Creation and update specification for <see cref="CommunicationTemplate" />.</summary>
public sealed record CommunicationTemplateSpec(
    string Name,
    string Key,
    CommunicationChannel Channel,
    string TopicKey = "general-announcements",
    string? SubjectTemplate = null,
    string? Description = null,
    string? PreheaderText = null,
    string? FromName = null,
    string? FromEmail = null,
    string? ReplyToEmail = null,
    Guid? SenderSystemPhoneNumberId = null,
    string? RazorLayoutKey = "Communication.StandardEmail",
    string? SmsBodyTemplate = null,
    string? PushTitleTemplate = null,
    string? PushBodyTemplate = null,
    IReadOnlyList<EmailSectionDto>? Sections = null,
    bool IsSystem = false,
    bool IsActive = true,
    Guid? SystemGuid = null
);
