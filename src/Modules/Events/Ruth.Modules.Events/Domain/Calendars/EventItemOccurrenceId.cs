using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="EventItemOccurrence" />.</summary>
public readonly record struct EventItemOccurrenceId(Guid Value)
    : IStronglyTypedId<EventItemOccurrenceId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static EventItemOccurrenceId Empty => new(Guid.Empty);

    public static EventItemOccurrenceId Create()
    {
        return new EventItemOccurrenceId(StronglyTypedId.NextGuid());
    }

    public static EventItemOccurrenceId From(Guid value)
    {
        return new EventItemOccurrenceId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
