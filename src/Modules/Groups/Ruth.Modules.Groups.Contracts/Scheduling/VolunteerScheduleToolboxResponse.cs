namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Volunteer Schedule Toolbox summary for a Person.</summary>
public sealed record VolunteerScheduleToolboxResponse(
    Guid PersonId,
    IReadOnlyList<ScheduledVolunteerResponse> UpcomingAssignments,
    IReadOnlyList<PersonScheduleExclusionResponse> BlackoutDates
);
