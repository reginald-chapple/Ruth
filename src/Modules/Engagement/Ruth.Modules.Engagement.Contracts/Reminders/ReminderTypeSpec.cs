namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Specification for creating a ReminderType.</summary>
/// <param name="Name">ReminderType display name.</param>
/// <param name="HighlightColorHex">Badge hex color.</param>
/// <param name="Description">Optional description.</param>
/// <param name="EntityTypeKey">Optional entity type key restriction (null allows any entity).</param>
/// <param name="ShouldAutoCompleteWhenNotified">Whether one-shot reminders auto-complete upon notification.</param>
/// <param name="NotificationFlavor">Notification delivery flavor.</param>
/// <param name="SystemCommunicationKey">Optional SystemCommunication template key.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record ReminderTypeSpec(
    string Name,
    string HighlightColorHex = "#0F6CBD",
    string? Description = null,
    string? EntityTypeKey = null,
    bool ShouldAutoCompleteWhenNotified = false,
    ReminderNotificationFlavor NotificationFlavor = ReminderNotificationFlavor.InAppBellOnly,
    string? SystemCommunicationKey = null,
    bool IsActive = true
);
