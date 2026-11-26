namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Concrete VEVENT occurrence compiled into an RFC 5545 iCalendar feed.</summary>
public sealed record IcsCalendarEventEntry(
    string Uid,
    string Summary,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateTimeOffset StampUtc,
    string? Description = null,
    string? Location = null,
    string? Url = null,
    IReadOnlyList<string>? Categories = null,
    bool IsCancelled = false
);
