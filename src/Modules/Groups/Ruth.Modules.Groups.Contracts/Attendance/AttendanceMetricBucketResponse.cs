namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Branch and program/headline attendance metric bucket aggregated across AttendanceOccurrences.</summary>
public sealed record AttendanceMetricBucketResponse(
    Guid? BranchId,
    DateOnly WeekEndDate,
    Guid? GroupTypeId,
    int OccurrenceCount,
    int DidNotOccurCount,
    int KnownAttendeeCount,
    int AnonymousAttendeeCount,
    int TotalAttendanceCount,
    int UniquePersonCount,
    int FirstTimeAttendeeCount,
    bool CountsTowardHeadlineAttendance
);
