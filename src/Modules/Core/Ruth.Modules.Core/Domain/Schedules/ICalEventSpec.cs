namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Parsed RFC 5545 VEVENT specification.</summary>
public sealed record ICalEventSpec(
    DateTime DtStart,
    bool DtStartIsUtc,
    bool IsAllDay,
    TimeSpan Duration,
    string? TimeZoneId,
    ICalRecurrenceRule? RRule,
    IReadOnlyList<DateTime> RDates,
    IReadOnlyList<DateTime> ExDates
);
