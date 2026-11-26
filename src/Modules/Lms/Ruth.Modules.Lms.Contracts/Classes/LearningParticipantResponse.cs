namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Enrolled Student or Facilitator in a LearningClass.</summary>
/// <param name="Id">Participant identifier.</param>
/// <param name="LearningClassId">Class identifier.</param>
/// <param name="PersonId">Constituent PersonId.</param>
/// <param name="Role">Student or Facilitator.</param>
/// <param name="CompletionStatus">Incomplete, Pass, or Fail.</param>
/// <param name="CompletionDateTimeUtc">Completion timestamp when finished.</param>
/// <param name="CurrentGradePercent">Current calculated grade percentage.</param>
/// <param name="CurrentGradeScaleName">Current matching grade scale label.</param>
/// <param name="EnrolledDateTimeUtc">Enrollment timestamp in UTC.</param>
/// <param name="CompletedActivitiesCount">Count of completed activities.</param>
/// <param name="TotalActivitiesCount">Total student activities in the class.</param>
public sealed record LearningParticipantResponse(
    Guid Id,
    Guid LearningClassId,
    Guid PersonId,
    LearningParticipantRole Role,
    LearningCompletionStatus CompletionStatus,
    DateTimeOffset? CompletionDateTimeUtc,
    decimal CurrentGradePercent,
    string? CurrentGradeScaleName,
    DateTimeOffset EnrolledDateTimeUtc,
    int CompletedActivitiesCount,
    int TotalActivitiesCount
);
