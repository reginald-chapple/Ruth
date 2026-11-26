namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>RFC 5545 iCalendar (.ics) feed payload for an event calendar.</summary>
public sealed record CalendarFeedPayloadResponse(
    string CalendarSlug,
    string ContentType,
    string IcsContent
);
