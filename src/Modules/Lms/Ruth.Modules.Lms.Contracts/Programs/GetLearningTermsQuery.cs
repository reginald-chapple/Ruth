using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Programs;

/// <summary>Lists LearningTerms for a LearningProgram.</summary>
/// <param name="LearningProgramId">Program identifier.</param>
public sealed record GetLearningTermsQuery(Guid LearningProgramId)
    : IQuery<IReadOnlyList<LearningTermResponse>>;
