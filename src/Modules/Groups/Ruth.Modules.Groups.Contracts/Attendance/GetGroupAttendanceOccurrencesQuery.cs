using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Lists AttendanceOccurrences for a Group over an optional date range.</summary>
public sealed record GetGroupAttendanceOccurrencesQuery(
    Guid GroupId,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int MaxResults = 50
) : IQuery<IReadOnlyList<AttendanceOccurrenceSummaryResponse>>;
