namespace Ruth.Modules.Lms.Contracts.Grading;

/// <summary>Persisted LearningGradingSystem with ordered scale bands.</summary>
/// <param name="Id">Grading system identifier.</param>
/// <param name="Name">Name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IsActive">Active status.</param>
/// <param name="Scales">Ordered grade bands (descending threshold).</param>
public sealed record LearningGradingSystemResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<LearningGradingScaleResponse> Scales
);
