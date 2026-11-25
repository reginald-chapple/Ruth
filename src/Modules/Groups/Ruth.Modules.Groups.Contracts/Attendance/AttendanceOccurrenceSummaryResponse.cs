namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Summary of an AttendanceOccurrence for listing screens.</summary>
public sealed record AttendanceOccurrenceSummaryResponse(
    Guid Id,
    Guid? GroupId,
    string? GroupName,
    Guid? RootGroupTypeId,
    Guid? LocationId,
    Guid? ScheduleId,
    DateOnly OccurrenceDate,
    DateOnly SundayDate,
    int OccurrenceDateKey,
    string? Name,
    bool? DidNotOccur,
    int? AnonymousAttendanceCount,
    int PresentCount,
    int TotalRecordedCount,
    int FirstTimeCount,
    string? Notes
);
