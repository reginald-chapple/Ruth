namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Parsed RFC 5545 RRULE specification.</summary>
public sealed record ICalRecurrenceRule(
    RecurrenceFrequency Frequency,
    int Interval,
    int? Count,
    DateTime? Until,
    bool UntilIsUtc,
    IReadOnlyList<ByDayRule> ByDays,
    IReadOnlyList<int> ByMonthDays,
    IReadOnlyList<int> ByMonths,
    IReadOnlyList<int> BySetPos,
    DayOfWeek WeekStart
);
