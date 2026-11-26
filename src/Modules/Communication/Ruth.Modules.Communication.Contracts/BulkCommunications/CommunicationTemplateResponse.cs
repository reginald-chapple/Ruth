using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Reusable bulk communication template projection including structured email sections.</summary>
/// <param name="Id">Unique template identifier.</param>
/// <param name="Name">Human-readable template title.</param>
/// <param name="Key">Unique slug key.</param>
/// <param name="Description">Optional administrative description.</param>
/// <param name="Channel">Primary communication channel.</param>
/// <param name="TopicKey">Default preference topic key.</param>
/// <param name="SubjectTemplate">Email subject merge template.</param>
/// <param name="PreheaderText">Optional email inbox preheader text.</param>
/// <param name="FromName">Optional sender display name override.</param>
/// <param name="FromEmail">Optional sender email address override.</param>
/// <param name="ReplyToEmail">Optional reply-to email address.</param>
/// <param name="SenderSystemPhoneNumberId">Optional outbound SMS phone number identifier.</param>
/// <param name="RazorLayoutKey">Optional compiled Razor layout key.</param>
/// <param name="SmsBodyTemplate">Optional plain-text SMS body template.</param>
/// <param name="PushTitleTemplate">Optional push notification title template.</param>
/// <param name="PushBodyTemplate">Optional push notification body template.</param>
/// <param name="Sections">Ordered structured email composer sections.</param>
/// <param name="IsSystem">Whether this template is system-seeded and protected from deletion.</param>
/// <param name="IsActive">Whether this template is active for new campaigns.</param>
/// <param name="CreatedOnUtc">Creation timestamp (UTC).</param>
/// <param name="ModifiedOnUtc">Last modification timestamp (UTC).</param>
public sealed record CommunicationTemplateResponse(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    CommunicationChannel Channel,
    string TopicKey,
    string? SubjectTemplate,
    string? PreheaderText,
    string? FromName,
    string? FromEmail,
    string? ReplyToEmail,
    Guid? SenderSystemPhoneNumberId,
    string? RazorLayoutKey,
    string? SmsBodyTemplate,
    string? PushTitleTemplate,
    string? PushBodyTemplate,
    IReadOnlyList<EmailSectionDto> Sections,
    bool IsSystem,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
