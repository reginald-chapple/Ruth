using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Portal;

/// <summary>Loads a learner's enrolled classes and completion progress for the Learner Portal.</summary>
/// <param name="PersonId">Learner PersonId.</param>
public sealed record GetLearnerPortalQuery(Guid PersonId)
    : IQuery<IReadOnlyList<LearnerClassWorkspaceResponse>>;
