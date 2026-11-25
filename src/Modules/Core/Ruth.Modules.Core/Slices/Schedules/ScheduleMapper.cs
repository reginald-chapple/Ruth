using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Schedules;

namespace Ruth.Modules.Core.Slices.Schedules;

internal static class ScheduleMapper
{
    public static ScheduleResponse ToResponse(Schedule schedule)
    {
        return new ScheduleResponse(
            schedule.Id.Value,
            schedule.SystemGuid,
            schedule.Name,
            schedule.AbbreviatedName,
            schedule.Description,
            schedule.ICalendarContent,
            schedule.CheckInStartOffsetMinutes,
            schedule.CheckInEndOffsetMinutes,
            schedule.EffectiveStartDate,
            schedule.EffectiveEndDate,
            schedule.WeeklyDayOfWeek,
            schedule.WeeklyTimeOfDay,
            schedule.CategoryId?.Value,
            schedule.AutoInactivateWhenComplete,
            schedule.IsActive,
            schedule.IsPublic,
            schedule.Order,
            schedule.GetKind(),
            schedule.ToFriendlyText(),
            schedule.GetDurationMinutes(),
            schedule.HasScheduleWarning(),
            schedule.CreatedOnUtc,
            schedule.ModifiedOnUtc
        );
    }

    public static ScheduleCategoryExclusionResponse ToResponse(ScheduleCategoryExclusion exclusion)
    {
        return new ScheduleCategoryExclusionResponse(
            exclusion.Id.Value,
            exclusion.CategoryId.Value,
            exclusion.Title,
            exclusion.StartDate,
            exclusion.EndDate,
            exclusion.StartOnUtc,
            exclusion.EndOnUtc,
            exclusion.CreatedOnUtc
        );
    }

    public static ScheduleInput ToInput(CreateScheduleCommand cmd)
    {
        return new ScheduleInput(
            cmd.Name,
            cmd.AbbreviatedName,
            cmd.Description,
            cmd.ICalendarContent,
            cmd.CheckInStartOffsetMinutes,
            cmd.CheckInEndOffsetMinutes,
            cmd.WeeklyDayOfWeek,
            cmd.WeeklyTimeOfDay,
            cmd.CategoryId.HasValue ? CategoryId.From(cmd.CategoryId.Value) : null,
            cmd.AutoInactivateWhenComplete,
            cmd.IsActive,
            cmd.IsPublic,
            cmd.Order
        );
    }

    public static ScheduleInput ToInput(UpdateScheduleCommand cmd)
    {
        return new ScheduleInput(
            cmd.Name,
            cmd.AbbreviatedName,
            cmd.Description,
            cmd.ICalendarContent,
            cmd.CheckInStartOffsetMinutes,
            cmd.CheckInEndOffsetMinutes,
            cmd.WeeklyDayOfWeek,
            cmd.WeeklyTimeOfDay,
            cmd.CategoryId.HasValue ? CategoryId.From(cmd.CategoryId.Value) : null,
            cmd.AutoInactivateWhenComplete,
            cmd.IsActive,
            cmd.IsPublic,
            cmd.Order
        );
    }
}
