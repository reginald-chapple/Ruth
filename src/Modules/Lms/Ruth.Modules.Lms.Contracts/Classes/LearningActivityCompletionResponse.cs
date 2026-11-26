namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Learner completion record for a specific LearningActivity.</summary>
/// <param name="Id">Completion identifier.</param>
/// <param name="LearningClassId">Class identifier.</param>
/// <param name="LearningActivityId">Activity identifier.</param>
/// <param name="ActivityName">Activity display title.</param>
/// <param name="ComponentType">Activity kind.</param>
/// <param name="LearningParticipantId">Participant identifier.</param>
/// <param name="PersonId">Learner PersonId.</param>
/// <param name="IsStudentCompleted">Whether submitted by the learner.</param>
/// <param name="IsFacilitatorCompleted">Whether reviewed/completed by facilitator.</param>
/// <param name="RequiresGrading">Whether awaiting facilitator grading.</param>
/// <param name="PointsEarned">Awarded points.</param>
/// <param name="PointsPossible">Maximum activity points.</param>
/// <param name="WasCompletedOnTime">Whether completed on or before due date.</param>
/// <param name="CompletedDateTimeUtc">Completion timestamp.</param>
/// <param name="StudentComment">Optional learner comment.</param>
/// <param name="FacilitatorComment">Optional facilitator feedback.</param>
/// <param name="BinaryFileId">Optional uploaded file identifier.</param>
/// <param name="GradedByPersonId">Optional grading facilitator PersonId.</param>
/// <param name="GradedDateTimeUtc">Optional graded timestamp.</param>
public sealed record LearningActivityCompletionResponse(
    Guid Id,
    Guid LearningClassId,
    Guid LearningActivityId,
    string ActivityName,
    LearningActivityComponentType ComponentType,
    Guid LearningParticipantId,
    Guid PersonId,
    bool IsStudentCompleted,
    bool IsFacilitatorCompleted,
    bool RequiresGrading,
    int PointsEarned,
    int PointsPossible,
    bool WasCompletedOnTime,
    DateTimeOffset? CompletedDateTimeUtc,
    string? StudentComment,
    string? FacilitatorComment,
    Guid? BinaryFileId,
    Guid? GradedByPersonId,
    DateTimeOffset? GradedDateTimeUtc
);
