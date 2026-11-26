namespace Ruth.Modules.Lms.Contracts.Grading;

/// <summary>Grade band inside a LearningGradingSystem (e.g., A >= 90%, Pass >= 70%).</summary>
/// <param name="Name">Band label.</param>
/// <param name="ThresholdPercentage">Minimum percentage (0-100) required to achieve this band.</param>
/// <param name="IsPassing">Whether this grade counts as passing.</param>
/// <param name="HighlightColor">Optional CSS color token.</param>
/// <param name="Description">Optional description.</param>
public sealed record LearningGradingScaleSpec(
    string Name,
    decimal ThresholdPercentage,
    bool IsPassing = true,
    string? HighlightColor = null,
    string? Description = null
);
