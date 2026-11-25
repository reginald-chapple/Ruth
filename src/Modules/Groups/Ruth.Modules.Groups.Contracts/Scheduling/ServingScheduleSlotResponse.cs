namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Single slot cell in the Coordinator Schedule Grid (Location x Schedule x Date).</summary>
public sealed record ServingScheduleSlotResponse(
    Guid GroupId,
    Guid? LocationId,
    string LocationName,
    Guid? ScheduleId,
    DateOnly OccurrenceDate,
    bool IsExcludedByGroupType,
    int? MinimumCapacity,
    int? DesiredCapacity,
    int? MaximumCapacity,
    int ConfirmedCount,
    int PendingCount,
    int DeclinedCount,
    ScheduleCapacityStatus CapacityStatus,
    IReadOnlyList<ScheduledVolunteerResponse> Volunteers
);
