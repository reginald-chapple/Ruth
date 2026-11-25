using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Computes Campus/Service attendance metrics across a date range.</summary>
public sealed record GetAttendanceMetricsSummaryQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? CampusId = null,
    Guid? GroupTypeId = null,
    Guid? GroupId = null,
    bool WeekendServicesOnly = false
) : IQuery<AttendanceMetricsSummaryResponse>;
