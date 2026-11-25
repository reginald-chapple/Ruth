using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Updates an existing schedule and refreshes its materialized ScheduleDate horizon.</summary>
public sealed record UpdateScheduleCommand(
    Guid ScheduleId,
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
