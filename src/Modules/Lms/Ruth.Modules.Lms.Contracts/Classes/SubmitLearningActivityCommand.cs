using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Submits a learner completion for a LearningActivity, auto-grading objective assessments or queueing for facilitator review.</summary>
/// <param name="LearningClassId">Class identifier.</param>
/// <param name="LearningActivityId">Activity identifier.</param>
/// <param name="PersonId">Learner PersonId.</param>
/// <param name="SubmissionDataJson">Optional structured submission payload (e.g., quiz answers or acknowledgment flag).</param>
/// <param name="BinaryFileId">Optional RustFS BinaryFileId for FileUpload activities.</param>
/// <param name="StudentComment">Optional learner comment.</param>
public sealed record SubmitLearningActivityCommand(
    Guid LearningClassId,
    Guid LearningActivityId,
    Guid PersonId,
    string? SubmissionDataJson = null,
    Guid? BinaryFileId = null,
    string? StudentComment = null
) : ICommand<LearningActivityCompletionResponse>;
