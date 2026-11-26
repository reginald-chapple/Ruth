using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Grading;

/// <summary>Creates a LearningGradingSystem with ordered grade bands.</summary>
/// <param name="Name">System name (e.g., Standard Letter Grades, Pass/Fail Certification).</param>
/// <param name="Scales">Ordered grade bands.</param>
/// <param name="Description">Optional description.</param>
public sealed record CreateLearningGradingSystemCommand(
    string Name,
    IReadOnlyList<LearningGradingScaleSpec> Scales,
    string? Description = null
) : ICommand<LearningGradingSystemResponse>;
