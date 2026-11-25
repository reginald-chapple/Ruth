using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Groups.Contracts.Events;

/// <summary>
/// Outbox integration event published when a person's attendance (DidAttend = true) is recorded
/// for an AttendanceOccurrence, enabling downstream modules (Streaks, Achievements, Metrics, Workflows) to react.
/// </summary>
public sealed record PersonAttendedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid AttendanceId,
    Guid AttendanceOccurrenceId,
    Guid PersonId,
    Guid? GroupId,
    Guid? GroupTypeId,
    Guid? CampusId,
    Guid? LocationId,
    Guid? ScheduleId,
    DateOnly OccurrenceDate,
    DateOnly SundayDate,
    bool IsFirstTime,
    bool CountsAsWeekendService
) : IIntegrationEvent;
