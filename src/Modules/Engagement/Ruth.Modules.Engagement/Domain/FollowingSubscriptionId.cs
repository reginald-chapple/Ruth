using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a FollowingSubscription.</summary>
public readonly record struct FollowingSubscriptionId(Guid Value)
    : IStronglyTypedId<FollowingSubscriptionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static FollowingSubscriptionId Empty => new(Guid.Empty);
    public static FollowingSubscriptionId Create() => new(StronglyTypedId.NextGuid());
    public static FollowingSubscriptionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
