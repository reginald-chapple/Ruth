using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Grades a learner's activity submission from the Facilitator Grading Queue and recalculates course grade/completion.</summary>
/// <param name="CompletionId">LearningActivityCompletion identifier.</param>
/// <param name="PointsEarned">Points awarded by the facilitator.</param>
/// <param name="FacilitatorPersonId">Grading facilitator PersonId.</param>
/// <param name="FacilitatorComment">Optional feedback comment.</param>
public sealed record GradeLearningActivityCommand(
    Guid CompletionId,
    int PointsEarned,
    Guid FacilitatorPersonId,
    string? FacilitatorComment = null
) : ICommand<LearningActivityCompletionResponse>;
