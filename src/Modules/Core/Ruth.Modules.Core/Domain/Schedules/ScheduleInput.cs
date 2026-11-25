namespace Ruth.Modules.Core.Domain.Schedules;

internal sealed record ScheduleInput(
    string? Name,
    string? AbbreviatedName,
    string? Description,
    string? ICalendarContent,
    int? CheckInStartOffsetMinutes,
    int? CheckInEndOffsetMinutes,
    DayOfWeek? WeeklyDayOfWeek,
    TimeOnly? WeeklyTimeOfDay,
    CategoryId? CategoryId,
    bool AutoInactivateWhenComplete,
    bool IsActive,
    bool IsPublic,
    int Order
);
