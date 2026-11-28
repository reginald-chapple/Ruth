namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Individual scheduled volunteer entry in the Coordinator Schedule Grid or Volunteer Toolbox.</summary>
public sealed record ScheduledVolunteerResponse(
    Guid AttendanceId,
    Guid AttendanceOccurrenceId,
    Guid GroupId,
    string GroupName,
    Guid? LocationId,
    string? LocationName,
    Guid? ScheduleId,
    DateOnly OccurrenceDate,
    Guid PersonId,
    bool RequestedToAttend,
    bool ScheduledToAttend,
    RsvpStatus Rsvp,
    DateTimeOffset? RsvpDateTimeUtc,
    string? RsvpToken,
    Guid? DeclineReasonValueId,
    string? DeclineReasonNote,
    bool ScheduleConfirmationSent,
    bool ScheduleReminderSent,
    bool HasBlackoutConflict,
    bool HasDoubleBookingConflict,
    bool MeetsRequirements,
    string? CanonicalRsvpUrl = null
);
