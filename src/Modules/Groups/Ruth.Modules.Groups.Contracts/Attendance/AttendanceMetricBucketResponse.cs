namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Campus and weekend-service metric bucket aggregated across AttendanceOccurrences.</summary>
public sealed record AttendanceMetricBucketResponse(
    Guid? CampusId,
    DateOnly SundayDate,
    Guid? GroupTypeId,
    int OccurrenceCount,
    int DidNotOccurCount,
    int KnownAttendeeCount,
    int AnonymousAttendeeCount,
    int TotalAttendanceCount,
    int UniquePersonCount,
    int FirstTimeAttendeeCount,
    bool CountsAsWeekendService
);
