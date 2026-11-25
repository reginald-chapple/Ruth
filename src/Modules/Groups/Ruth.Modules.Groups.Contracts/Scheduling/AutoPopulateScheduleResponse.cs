namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Summary returned after auto-populating serving schedule slots.</summary>
public sealed record AutoPopulateScheduleResponse(
    Guid GroupId,
    DateOnly OccurrenceDate,
    int SlotsEvaluated,
    int VolunteersScheduled,
    IReadOnlyList<ScheduledVolunteerResponse> CreatedAssignments
);
