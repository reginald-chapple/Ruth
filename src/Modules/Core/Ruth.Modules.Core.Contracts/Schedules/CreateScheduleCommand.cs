using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Creates a new schedule and materializes its horizon ScheduleDate rows.</summary>
public sealed record CreateScheduleCommand(
    string? Name = null,
    string? AbbreviatedName = null,
    string? Description = null,
    string? ICalendarContent = null,
    int? CheckInStartOffsetMinutes = null,
    int? CheckInEndOffsetMinutes = null,
    DayOfWeek? WeeklyDayOfWeek = null,
    TimeOnly? WeeklyTimeOfDay = null,
    Guid? CategoryId = null,
    bool AutoInactivateWhenComplete = false,
    bool IsActive = true,
    bool IsPublic = true,
    int Order = 0,
    string? TimeZoneId = null
) : ICommand<ScheduleResponse>;
