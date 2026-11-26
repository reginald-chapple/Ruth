using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="InteractiveAction" />.</summary>
public readonly record struct InteractiveActionId(Guid Value)
    : IStronglyTypedId<InteractiveActionId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static InteractiveActionId Empty => new(Guid.Empty);

    public static InteractiveActionId Create()
    {
        return new InteractiveActionId(StronglyTypedId.NextGuid());
    }

    public static InteractiveActionId From(Guid value)
    {
        return new InteractiveActionId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
