namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Read model representing a transactional system communication template.</summary>
/// <param name="Id">Unique system communication identifier.</param>
/// <param name="SystemGuid">Deterministic system GUID.</param>
/// <param name="Title">Human-readable template title.</param>
/// <param name="Key">Unique slug key (e.g., identity.password-reset).</param>
/// <param name="Category">Functional module category.</param>
/// <param name="FromName">Optional sender display name override.</param>
/// <param name="FromEmail">Optional sender email address override.</param>
/// <param name="SubjectTemplate">Declarative merge subject line template.</param>
/// <param name="HtmlBodyTemplate">Declarative merge HTML body template.</param>
/// <param name="RazorLayoutKey">Optional registered compiled Razor layout key (e.g., Communication.StandardEmail).</param>
/// <param name="SmsFromSystemPhoneNumberId">Optional sender SystemPhoneNumber identifier for SMS dispatch.</param>
/// <param name="SmsMessageTemplate">Optional SMS plain-text merge template.</param>
/// <param name="PushTitleTemplate">Optional push notification title merge template.</param>
/// <param name="PushMessageTemplate">Optional push notification body merge template.</param>
/// <param name="PushOpenUrlTemplate">Optional push notification action URL merge template.</param>
/// <param name="IsSystem">Whether this definition is a protected built-in template.</param>
/// <param name="IsActive">Whether dispatch is enabled for this definition.</param>
public sealed record SystemCommunicationResponse(
    Guid Id,
    Guid SystemGuid,
    string Title,
    string Key,
    SystemCommunicationCategory Category,
    string? FromName,
    string? FromEmail,
    string SubjectTemplate,
    string HtmlBodyTemplate,
    string? RazorLayoutKey,
    Guid? SmsFromSystemPhoneNumberId,
    string? SmsMessageTemplate,
    string? PushTitleTemplate,
    string? PushMessageTemplate,
    string? PushOpenUrlTemplate,
    bool IsSystem,
    bool IsActive
);
