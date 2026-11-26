namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Delivery channels triggered when a Reminder reaches its due date.</summary>
public enum ReminderNotificationFlavor
{
    /// <summary>Creates an in-app NotificationMessage bell item only.</summary>
    InAppBellOnly = 0,

    /// <summary>Creates an in-app NotificationMessage and dispatches a SystemCommunication template.</summary>
    InAppAndSystemNotification = 1,
}
