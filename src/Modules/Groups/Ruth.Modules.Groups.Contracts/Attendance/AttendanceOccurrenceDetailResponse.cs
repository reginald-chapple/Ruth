namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Detailed AttendanceOccurrence including all recorded roster attendances.</summary>
public sealed record AttendanceOccurrenceDetailResponse(
    Guid Id,
    Guid? GroupId,
    string? GroupName,
    Guid? RootGroupTypeId,
    Guid? LocationId,
    Guid? ScheduleId,
    DateOnly OccurrenceDate,
    DateOnly WeekEndDate,
    int OccurrenceDateKey,
    string? Name,
    bool? DidNotOccur,
    int? AnonymousAttendanceCount,
    int PresentCount,
    int FirstTimeCount,
    string? Notes,
    IReadOnlyList<AttendanceRecordResponse> Attendances
);
