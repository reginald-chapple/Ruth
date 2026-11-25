using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Attendance;

/// <summary>Records or updates a Group AttendanceOccurrence and roster checklist, staging PersonAttendedIntegrationEvents.</summary>
public sealed record RecordGroupAttendanceCommand(
    Guid GroupId,
    DateOnly OccurrenceDate,
    Guid? LocationId = null,
    Guid? ScheduleId = null,
    bool DidNotOccur = false,
    int? AnonymousAttendanceCount = null,
    string? Notes = null,
    string? Name = null,
    Guid? CheckedInByPersonId = null,
    IReadOnlyList<GroupAttendanceEntryInputDto>? Entries = null
) : ICommand<AttendanceOccurrenceDetailResponse>;
