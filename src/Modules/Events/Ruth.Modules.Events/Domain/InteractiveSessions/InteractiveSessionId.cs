using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="InteractiveSession" />.</summary>
public readonly record struct InteractiveSessionId(Guid Value)
    : IStronglyTypedId<InteractiveSessionId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static InteractiveSessionId Empty => new(Guid.Empty);

    public static InteractiveSessionId Create()
    {
        return new InteractiveSessionId(StronglyTypedId.NextGuid());
    }

    public static InteractiveSessionId From(Guid value)
    {
        return new InteractiveSessionId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
