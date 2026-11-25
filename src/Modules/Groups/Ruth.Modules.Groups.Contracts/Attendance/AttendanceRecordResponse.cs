using Ruth.Modules.Groups.Contracts.Scheduling;

namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Individual recorded Attendance line inside an AttendanceOccurrence.</summary>
public sealed record AttendanceRecordResponse(
    Guid Id,
    Guid OccurrenceId,
    Guid? PersonId,
    Guid? CampusId,
    Guid? DeviceId,
    DateTimeOffset StartDateTimeUtc,
    DateTimeOffset? EndDateTimeUtc,
    bool? DidAttend,
    bool? IsFirstTime,
    string? Note,
    RsvpStatus Rsvp,
    bool? RequestedToAttend,
    bool? ScheduledToAttend
);
