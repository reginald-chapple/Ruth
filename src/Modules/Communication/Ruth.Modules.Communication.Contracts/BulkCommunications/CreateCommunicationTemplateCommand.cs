using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Creates a reusable CommunicationTemplate with optional structured email sections.</summary>
/// <param name="Name">Human-readable template name.</param>
/// <param name="Key">Unique slug key (lowercase letters, digits, dots, hyphens, underscores).</param>
/// <param name="Channel">Target communication channel.</param>
/// <param name="TopicKey">Default preference center topic key.</param>
/// <param name="SubjectTemplate">Email subject merge template (required when channel is Email).</param>
/// <param name="Description">Optional description.</param>
/// <param name="PreheaderText">Optional email preheader text.</param>
/// <param name="FromName">Optional sender display name.</param>
/// <param name="FromEmail">Optional sender email address.</param>
/// <param name="ReplyToEmail">Optional reply-to email address.</param>
/// <param name="SenderSystemPhoneNumberId">Optional outbound SMS phone number identifier.</param>
/// <param name="RazorLayoutKey">Optional compiled Razor layout key.</param>
/// <param name="SmsBodyTemplate">Optional SMS plain-text merge template.</param>
/// <param name="PushTitleTemplate">Optional push notification title template.</param>
/// <param name="PushBodyTemplate">Optional push notification body template.</param>
/// <param name="Sections">Ordered structured email composer sections.</param>
/// <param name="IsActive">Whether the template is active.</param>
public sealed record CreateCommunicationTemplateCommand(
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
    bool IsActive = true
) : ICommand<CommunicationTemplateResponse>;
