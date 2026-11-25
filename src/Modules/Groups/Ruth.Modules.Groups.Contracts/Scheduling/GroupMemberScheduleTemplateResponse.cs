namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Reusable volunteer rotation cadence template (e.g., Every Week, Every Other Week).</summary>
public sealed record GroupMemberScheduleTemplateResponse(
    Guid Id,
    string Name,
    Guid? GroupTypeId,
    Guid ScheduleId,
    string RecurrenceDescription,
    int IntervalWeeks
);
