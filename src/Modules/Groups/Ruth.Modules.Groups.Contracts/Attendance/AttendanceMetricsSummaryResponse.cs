namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Aggregated Campus/Service attendance metrics summary for reporting and dashboard hooks.</summary>
public sealed record AttendanceMetricsSummaryResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalKnownAttendance,
    int TotalAnonymousAttendance,
    int TotalAttendance,
    int TotalUniquePeople,
    int TotalFirstTimeAttendees,
    int WeekendServiceTotalAttendance,
    IReadOnlyList<AttendanceMetricBucketResponse> Buckets
);
