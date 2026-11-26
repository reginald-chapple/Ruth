using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Reminders;

/// <summary>Query to retrieve in-app NotificationMessages for the Notification Bell.</summary>
/// <param name="RecipientPersonId">Optional recipient Person filter (null returns latest system-wide in staff view).</param>
/// <param name="UnreadOnly">Whether to return only unread notifications.</param>
public sealed record GetPersonNotificationsQuery(
    Guid? RecipientPersonId = null,
    bool UnreadOnly = false
) : IQuery<IReadOnlyList<NotificationMessageResponse>>;
