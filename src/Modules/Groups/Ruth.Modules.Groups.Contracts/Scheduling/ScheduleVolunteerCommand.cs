using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Schedules (requests or auto-confirms) a volunteer for a specific Group, Location, Schedule, and Date.</summary>
public sealed record ScheduleVolunteerCommand(
    Guid GroupId,
    Guid PersonId,
    DateOnly OccurrenceDate,
    Guid? LocationId = null,
    Guid? ScheduleId = null,
    Guid? ScheduledByPersonId = null,
    bool ForceOverrideConflicts = false
) : ICommand<ScheduledVolunteerResponse>;
