using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Computes Branch and program attendance metrics across a date range.</summary>
public sealed record GetAttendanceMetricsSummaryQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? BranchId = null,
    Guid? GroupTypeId = null,
    Guid? GroupId = null,
    bool HeadlineAttendanceOnly = false
) : IQuery<AttendanceMetricsSummaryResponse>;
