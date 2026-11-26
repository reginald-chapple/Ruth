using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a FollowingEventNotification.</summary>
public readonly record struct FollowingEventNotificationId(Guid Value)
    : IStronglyTypedId<FollowingEventNotificationId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static FollowingEventNotificationId Empty => new(Guid.Empty);

    public static FollowingEventNotificationId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static FollowingEventNotificationId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
