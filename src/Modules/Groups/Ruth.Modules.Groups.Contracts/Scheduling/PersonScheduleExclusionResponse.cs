namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Personal or family-linked blackout date range for volunteer scheduling.</summary>
public sealed record PersonScheduleExclusionResponse(
    Guid Id,
    Guid PersonId,
    Guid? GroupId,
    Guid? ParentExclusionId,
    string? Title,
    DateOnly StartDate,
    DateOnly EndDate
);
