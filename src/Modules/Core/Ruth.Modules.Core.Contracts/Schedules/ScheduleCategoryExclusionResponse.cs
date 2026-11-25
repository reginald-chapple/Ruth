namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Represents a holiday or blackout exclusion range for all schedules in a category.</summary>
public sealed record ScheduleCategoryExclusionResponse(
    Guid Id,
    Guid CategoryId,
    string Title,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTimeOffset StartOnUtc,
    DateTimeOffset EndOnUtc,
    DateTimeOffset CreatedOnUtc
);
