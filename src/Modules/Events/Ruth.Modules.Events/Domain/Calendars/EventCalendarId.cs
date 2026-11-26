using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="EventCalendar" />.</summary>
public readonly record struct EventCalendarId(Guid Value) : IStronglyTypedId<EventCalendarId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static EventCalendarId Empty => new(Guid.Empty);

    public static EventCalendarId Create()
    {
        return new EventCalendarId(StronglyTypedId.NextGuid());
    }

    public static EventCalendarId From(Guid value)
    {
        return new EventCalendarId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
