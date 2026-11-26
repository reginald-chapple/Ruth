using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="EventItem" />.</summary>
public readonly record struct EventItemId(Guid Value) : IStronglyTypedId<EventItemId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static EventItemId Empty => new(Guid.Empty);

    public static EventItemId Create()
    {
        return new EventItemId(StronglyTypedId.NextGuid());
    }

    public static EventItemId From(Guid value)
    {
        return new EventItemId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
