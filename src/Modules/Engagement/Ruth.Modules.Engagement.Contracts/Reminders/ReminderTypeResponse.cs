namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Represents a configured ReminderType.</summary>
/// <param name="Id">ReminderType identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="EntityTypeKey">Optional entity type key filter.</param>
/// <param name="HighlightColorHex">Highlight color hex.</param>
/// <param name="ShouldAutoCompleteWhenNotified">Whether auto-completed on notify.</param>
/// <param name="NotificationFlavor">Notification delivery flavor.</param>
/// <param name="SystemCommunicationKey">Optional SystemCommunication key.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record ReminderTypeResponse(
    Guid Id,
    string Name,
    string? Description,
    string? EntityTypeKey,
    string HighlightColorHex,
    bool ShouldAutoCompleteWhenNotified,
    ReminderNotificationFlavor NotificationFlavor,
    string? SystemCommunicationKey,
    bool IsActive
);
