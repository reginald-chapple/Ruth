using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="InteractiveSessionOccurrence" />.</summary>
public readonly record struct InteractiveSessionOccurrenceId(Guid Value)
    : IStronglyTypedId<InteractiveSessionOccurrenceId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static InteractiveSessionOccurrenceId Empty => new(Guid.Empty);

    public static InteractiveSessionOccurrenceId Create()
    {
        return new InteractiveSessionOccurrenceId(StronglyTypedId.NextGuid());
    }

    public static InteractiveSessionOccurrenceId From(Guid value)
    {
        return new InteractiveSessionOccurrenceId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
