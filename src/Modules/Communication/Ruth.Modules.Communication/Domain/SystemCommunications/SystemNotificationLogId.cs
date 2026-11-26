using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.SystemCommunications;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SystemNotificationLog" />.</summary>
public readonly record struct SystemNotificationLogId(Guid Value)
    : IStronglyTypedId<SystemNotificationLogId>
{
    public static SystemNotificationLogId Empty => new(Guid.Empty);

    public static SystemNotificationLogId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static SystemNotificationLogId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
