using Ruth.Modules.Communication.Contracts.SystemCommunications;

namespace Ruth.Modules.Communication.Domain.SystemCommunications;

/// <summary>Validated creation/update specification for <see cref="SystemCommunication" />.</summary>
public sealed record SystemCommunicationSpec(
    string Title,
    string Key,
    SystemCommunicationCategory Category,
    string SubjectTemplate,
    string HtmlBodyTemplate,
    string? FromName = null,
    string? FromEmail = null,
    string? RazorLayoutKey = "Communication.StandardEmail",
    Guid? SmsFromSystemPhoneNumberId = null,
    string? SmsMessageTemplate = null,
    string? PushTitleTemplate = null,
    string? PushMessageTemplate = null,
    string? PushOpenUrlTemplate = null,
    bool IsSystem = false,
    bool IsActive = true,
    Guid? SystemGuid = null
);
