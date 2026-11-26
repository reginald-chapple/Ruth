using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="InteractiveResponse" />.</summary>
public readonly record struct InteractiveResponseId(Guid Value)
    : IStronglyTypedId<InteractiveResponseId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static InteractiveResponseId Empty => new(Guid.Empty);

    public static InteractiveResponseId Create()
    {
        return new InteractiveResponseId(StronglyTypedId.NextGuid());
    }

    public static InteractiveResponseId From(Guid value)
    {
        return new InteractiveResponseId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
