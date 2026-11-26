using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Domain.Following;

/// <summary>Notable entity event staged for consolidated Following digest notifications.</summary>
public sealed class FollowingEventNotification
    : AggregateRoot<FollowingEventNotificationId>,
        IAuditableEntity
{
    private FollowingEventNotification() { }

    private FollowingEventNotification(FollowingEventNotificationId id)
        : base(id) { }

    public string EntityTypeKey { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string EventTypeKey { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset? DigestProcessedAtUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public static Result<FollowingEventNotification> Create(
        string entityTypeKey,
        Guid entityId,
        string eventTypeKey,
        string summary,
        DateTimeOffset occurredAtUtc
    )
    {
        var invalid =
            string.IsNullOrWhiteSpace(entityTypeKey)
            || entityId == Guid.Empty
            || string.IsNullOrWhiteSpace(eventTypeKey)
            || string.IsNullOrWhiteSpace(summary);
        return invalid
            ? EngagementErrors.FollowingInvalid
            : new FollowingEventNotification(FollowingEventNotificationId.Create())
            {
                EntityTypeKey = entityTypeKey.Trim(),
                EntityId = entityId,
                EventTypeKey = eventTypeKey.Trim(),
                Summary = summary.Trim(),
                OccurredAtUtc = occurredAtUtc.ToUniversalTime(),
            };
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        DigestProcessedAtUtc = processedAtUtc.ToUniversalTime();
    }
}
