using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Retrieves pending submissions awaiting grading in the Facilitator Grading Queue.</summary>
/// <param name="LearningClassId">Optional class filter.</param>
public sealed record GetFacilitatorGradingQueueQuery(Guid? LearningClassId = null)
    : IQuery<IReadOnlyList<LearningActivityCompletionResponse>>;
