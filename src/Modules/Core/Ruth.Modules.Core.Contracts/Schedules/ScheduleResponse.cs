namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Detailed representation of a reusable or custom schedule.</summary>
public sealed record ScheduleResponse(
    Guid Id,
    Guid SystemGuid,
    string? Name,
    string? AbbreviatedName,
    string? Description,
    string? ICalendarContent,
    int? CheckInStartOffsetMinutes,
    int? CheckInEndOffsetMinutes,
    DateOnly? EffectiveStartDate,
    DateOnly? EffectiveEndDate,
    DayOfWeek? WeeklyDayOfWeek,
    TimeOnly? WeeklyTimeOfDay,
    Guid? CategoryId,
    bool AutoInactivateWhenComplete,
    bool IsActive,
    bool IsPublic,
    int Order,
    ScheduleKind Kind,
    string FriendlyText,
    int DurationMinutes,
    bool HasWarning,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
