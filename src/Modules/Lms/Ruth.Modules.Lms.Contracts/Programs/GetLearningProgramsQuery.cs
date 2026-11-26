using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Programs;

/// <summary>Lists LearningPrograms.</summary>
/// <param name="PublicOnly">When true, returns only public active programs.</param>
/// <param name="IncludeInactive">Whether to include inactive programs.</param>
public sealed record GetLearningProgramsQuery(bool PublicOnly = false, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<LearningProgramResponse>>;
