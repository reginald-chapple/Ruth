namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>GroupType-level date range during which serving occurrences do not run.</summary>
public sealed record GroupScheduleExclusionResponse(
    Guid Id,
    Guid GroupTypeId,
    string? Title,
    DateOnly StartDate,
    DateOnly EndDate
);
