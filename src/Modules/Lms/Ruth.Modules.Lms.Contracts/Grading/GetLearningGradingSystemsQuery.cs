using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Grading;

/// <summary>Lists configured LearningGradingSystems.</summary>
/// <param name="IncludeInactive">Whether to include inactive systems.</param>
public sealed record GetLearningGradingSystemsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<LearningGradingSystemResponse>>;
