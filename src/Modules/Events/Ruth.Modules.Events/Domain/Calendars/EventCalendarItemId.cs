using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="EventCalendarItem" /> join row.</summary>
public readonly record struct EventCalendarItemId(Guid Value)
    : IStronglyTypedId<EventCalendarItemId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static EventCalendarItemId Empty => new(Guid.Empty);

    public static EventCalendarItemId Create()
    {
        return new EventCalendarItemId(StronglyTypedId.NextGuid());
    }

    public static EventCalendarItemId From(Guid value)
    {
        return new EventCalendarItemId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
