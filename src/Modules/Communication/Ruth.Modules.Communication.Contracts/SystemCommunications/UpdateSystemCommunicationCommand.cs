using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Updates an existing transactional system communication template.</summary>
/// <param name="SystemCommunicationId">Target system communication identifier.</param>
/// <param name="Title">Human-readable template title.</param>
/// <param name="Key">Unique slug key.</param>
/// <param name="Category">Functional category.</param>
/// <param name="SubjectTemplate">Declarative merge subject line template.</param>
/// <param name="HtmlBodyTemplate">Declarative merge HTML body template.</param>
/// <param name="FromName">Optional sender display name.</param>
/// <param name="FromEmail">Optional sender email address.</param>
/// <param name="RazorLayoutKey">Optional registered Razor layout key.</param>
/// <param name="SmsFromSystemPhoneNumberId">Optional sender system phone number ID.</param>
/// <param name="SmsMessageTemplate">Optional SMS plain-text merge template.</param>
/// <param name="PushTitleTemplate">Optional push notification title merge template.</param>
/// <param name="PushMessageTemplate">Optional push notification body merge template.</param>
/// <param name="PushOpenUrlTemplate">Optional push notification action URL merge template.</param>
/// <param name="IsActive">Whether the template is active.</param>
public sealed record UpdateSystemCommunicationCommand(
    Guid SystemCommunicationId,
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
    bool IsActive = true
) : ICommand<SystemCommunicationResponse>;
