namespace Ruth.Modules.Lms.Contracts.Grading;

/// <summary>Persisted grade scale band.</summary>
/// <param name="Id">Scale identifier.</param>
/// <param name="Name">Band label.</param>
/// <param name="ThresholdPercentage">Minimum percentage threshold.</param>
/// <param name="IsPassing">Whether passing.</param>
/// <param name="HighlightColor">Optional color.</param>
/// <param name="Description">Optional description.</param>
public sealed record LearningGradingScaleResponse(
    Guid Id,
    string Name,
    decimal ThresholdPercentage,
    bool IsPassing,
    string? HighlightColor,
    string? Description
);
