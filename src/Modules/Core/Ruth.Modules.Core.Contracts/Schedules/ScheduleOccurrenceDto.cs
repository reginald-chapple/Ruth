namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Single materialized or evaluated schedule occurrence.</summary>
public sealed record ScheduleOccurrenceDto(
    DateTimeOffset StartOnUtc,
    DateTimeOffset EndOnUtc,
    DateTime LocalStart,
    DateTime LocalEnd,
    int StartDateKey
);
