namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Summary of due Reminders processed into NotificationMessages.</summary>
/// <param name="NotifiedCount">Number of due reminders notified.</param>
public sealed record ReminderProcessRunResponse(int NotifiedCount);
