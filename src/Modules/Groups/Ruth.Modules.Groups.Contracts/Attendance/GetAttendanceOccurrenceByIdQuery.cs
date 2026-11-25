using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Gets full AttendanceOccurrence details including all attendance records.</summary>
public sealed record GetAttendanceOccurrenceByIdQuery(Guid OccurrenceId)
    : IQuery<AttendanceOccurrenceDetailResponse>;
