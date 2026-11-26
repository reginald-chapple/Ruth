namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Represents an in-app NotificationMessage displayed in the Notification Bell.</summary>
/// <param name="Id">Notification identifier.</param>
/// <param name="RecipientPersonId">Recipient Person identifier.</param>
/// <param name="Title">Notification title.</param>
/// <param name="Message">Notification message body.</param>
/// <param name="Category">Notification category.</param>
/// <param name="ActionUrl">Optional target navigation URL.</param>
/// <param name="ColorHex">Optional accent color hex.</param>
/// <param name="IsRead">Whether read.</param>
/// <param name="ReadAtUtc">Optional read UTC timestamp.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record NotificationMessageResponse(
    Guid Id,
    Guid RecipientPersonId,
    string Title,
    string Message,
    NotificationCategory Category,
    string? ActionUrl,
    string? ColorHex,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedOnUtc
);
