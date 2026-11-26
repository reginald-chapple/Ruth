using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Adds a LearningActivity to a LearningClass.</summary>
/// <param name="LearningClassId">Class identifier.</param>
/// <param name="Name">Activity title.</param>
/// <param name="ComponentType">Video, Article, FileUpload, Assessment, or Acknowledgment.</param>
/// <param name="Points">Maximum points (0 or greater).</param>
/// <param name="Order">Sequence order.</param>
/// <param name="Description">Optional instructions.</param>
/// <param name="AssignTo">Student or Facilitator.</param>
/// <param name="AvailabilityCriteria">Unlock rule.</param>
/// <param name="AvailableDateDefault">Optional fixed available date.</param>
/// <param name="AvailableDateOffsetDays">Optional available offset days.</param>
/// <param name="DueDateCriteria">Due rule.</param>
/// <param name="DueDateDefault">Optional fixed due date.</param>
/// <param name="DueDateOffsetDays">Optional due offset days.</param>
/// <param name="IsStudentCommentingEnabled">Whether comments are allowed.</param>
/// <param name="ActivityConfigJson">Optional JSON configuration.</param>
public sealed record AddLearningActivityCommand(
    Guid LearningClassId,
    string Name,
    LearningActivityComponentType ComponentType,
    int Points = 100,
    int Order = 0,
    string? Description = null,
    LearningActivityAssignTo AssignTo = LearningActivityAssignTo.Student,
    ActivityAvailabilityCriteria AvailabilityCriteria =
        ActivityAvailabilityCriteria.AlwaysAvailable,
    DateTimeOffset? AvailableDateDefault = null,
    int? AvailableDateOffsetDays = null,
    ActivityDueDateCriteria DueDateCriteria = ActivityDueDateCriteria.NoDate,
    DateTimeOffset? DueDateDefault = null,
    int? DueDateOffsetDays = null,
    bool IsStudentCommentingEnabled = true,
    string? ActivityConfigJson = null
) : ICommand<LearningActivityResponse>;
