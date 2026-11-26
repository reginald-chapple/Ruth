using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Membership linking an <see cref="EventItem" /> to an <see cref="EventCalendar" />.</summary>
public sealed class EventCalendarItem : Entity<EventCalendarItemId>
{
    private EventCalendarItem() { }

    internal EventCalendarItem(
        EventCalendarItemId id,
        EventItemId eventItemId,
        EventCalendarId calendarId
    )
        : base(id)
    {
        EventItemId = eventItemId;
        CalendarId = calendarId;
    }

    public EventItemId EventItemId { get; private set; }
    public EventCalendarId CalendarId { get; private set; }
}
