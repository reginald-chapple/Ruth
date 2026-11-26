using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a NotificationMessage.</summary>
public readonly record struct NotificationMessageId(Guid Value)
    : IStronglyTypedId<NotificationMessageId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static NotificationMessageId Empty => new(Guid.Empty);
    public static NotificationMessageId Create() => new(StronglyTypedId.NextGuid());
    public static NotificationMessageId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
