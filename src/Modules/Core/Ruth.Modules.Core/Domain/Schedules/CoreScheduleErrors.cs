using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Schedules;

internal static class CoreScheduleErrors
{
    public static readonly DomainError ScheduleNotFound = DomainError.NotFound(
        "Core.ScheduleNotFound",
        "Schedule was not found."
    );

    public static readonly DomainError ScheduleNameTooLong = DomainError.Validation(
        "Core.ScheduleNameTooLong",
        "Schedule name cannot exceed 50 characters."
    );

    public static readonly DomainError ScheduleICalendarInvalid = DomainError.Validation(
        "Core.ScheduleICalendarInvalid",
        "Schedule iCalendar content is invalid or missing a valid DTSTART."
    );

    public static readonly DomainError ScheduleOffsetsInvalid = DomainError.Validation(
        "Core.ScheduleOffsetsInvalid",
        "Check-in offset minutes must be non-negative."
    );

    public static readonly DomainError ScheduleRangeInvalid = DomainError.Validation(
        "Core.ScheduleRangeInvalid",
        "Occurrence query range end must be greater than or equal to range start."
    );

    public static readonly DomainError ExclusionNotFound = DomainError.NotFound(
        "Core.ScheduleExclusionNotFound",
        "Schedule category exclusion was not found."
    );

    public static readonly DomainError ExclusionTitleRequired = DomainError.Validation(
        "Core.ScheduleExclusionTitleRequired",
        "Schedule exclusion title is required."
    );

    public static readonly DomainError ExclusionTitleTooLong = DomainError.Validation(
        "Core.ScheduleExclusionTitleTooLong",
        "Schedule exclusion title cannot exceed 50 characters."
    );

    public static readonly DomainError ExclusionDateRangeInvalid = DomainError.Validation(
        "Core.ScheduleExclusionDateRangeInvalid",
        "Schedule exclusion end date cannot be earlier than start date."
    );
}
