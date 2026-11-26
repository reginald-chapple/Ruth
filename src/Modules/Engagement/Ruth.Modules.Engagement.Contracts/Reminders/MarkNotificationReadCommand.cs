using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Command to mark an in-app NotificationMessage as read.</summary>
/// <param name="NotificationId">Target NotificationMessage identifier.</param>
public sealed record MarkNotificationReadCommand(Guid NotificationId)
    : ICommand<NotificationMessageResponse>;
