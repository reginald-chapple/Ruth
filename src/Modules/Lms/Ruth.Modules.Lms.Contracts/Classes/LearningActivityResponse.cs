namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>LearningActivity inside a LearningClass (Video, Article, FileUpload, Assessment, Acknowledgment).</summary>
/// <param name="Id">Activity identifier.</param>
/// <param name="LearningClassId">Owning class identifier.</param>
/// <param name="Name">Activity title.</param>
/// <param name="Description">Optional instructions.</param>
/// <param name="ComponentType">Activity component kind.</param>
/// <param name="AssignTo">Student or Facilitator.</param>
/// <param name="Order">Display sequence order.</param>
/// <param name="Points">Maximum possible points.</param>
/// <param name="AvailabilityCriteria">Unlock rule.</param>
/// <param name="AvailableDateDefault">Optional fixed available date.</param>
/// <param name="AvailableDateOffsetDays">Optional relative available offset days.</param>
/// <param name="DueDateCriteria">Due date rule.</param>
/// <param name="DueDateDefault">Optional fixed due date.</param>
/// <param name="DueDateOffsetDays">Optional relative due offset days.</param>
/// <param name="IsStudentCommentingEnabled">Whether learners can attach comments.</param>
/// <param name="ActivityConfigJson">Optional JSON config (video URL, article HTML, quiz answer key, etc.).</param>
public sealed record LearningActivityResponse(
    Guid Id,
    Guid LearningClassId,
    string Name,
    string? Description,
    LearningActivityComponentType ComponentType,
    LearningActivityAssignTo AssignTo,
    int Order,
    int Points,
    ActivityAvailabilityCriteria AvailabilityCriteria,
    DateTimeOffset? AvailableDateDefault,
    int? AvailableDateOffsetDays,
    ActivityDueDateCriteria DueDateCriteria,
    DateTimeOffset? DueDateDefault,
    int? DueDateOffsetDays,
    bool IsStudentCommentingEnabled,
    string? ActivityConfigJson
);
