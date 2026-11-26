using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain.Classes;

/// <summary>Individual activity (Video, Article, FileUpload, Assessment, Acknowledgment) inside a LearningClass.</summary>
public sealed class LearningActivity : Entity<LearningActivityId>
{
    public const string EntityTypeKeyName = "Ruth.Lms.LearningActivity";

    private LearningActivity() { }

    private LearningActivity(LearningActivityId id)
        : base(id) { }

    public LearningClassId LearningClassId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public LearningActivityComponentType ComponentType { get; private set; }
    public LearningActivityAssignTo AssignTo { get; private set; }
    public int Order { get; private set; }
    public int Points { get; private set; }
    public ActivityAvailabilityCriteria AvailabilityCriteria { get; private set; }
    public DateTimeOffset? AvailableDateDefault { get; private set; }
    public int? AvailableDateOffsetDays { get; private set; }
    public ActivityDueDateCriteria DueDateCriteria { get; private set; }
    public DateTimeOffset? DueDateDefault { get; private set; }
    public int? DueDateOffsetDays { get; private set; }
    public bool IsStudentCommentingEnabled { get; private set; } = true;
    public string? ActivityConfigJson { get; private set; }

    public static Result<LearningActivity> Create(
        LearningClassId classId,
        string name,
        LearningActivityComponentType componentType,
        int points,
        int order,
        string? description = null,
        LearningActivityAssignTo assignTo = LearningActivityAssignTo.Student,
        ActivityAvailabilityCriteria availabilityCriteria =
            ActivityAvailabilityCriteria.AlwaysAvailable,
        DateTimeOffset? availableDateDefault = null,
        int? availableDateOffsetDays = null,
        ActivityDueDateCriteria dueDateCriteria = ActivityDueDateCriteria.NoDate,
        DateTimeOffset? dueDateDefault = null,
        int? dueDateOffsetDays = null,
        bool isStudentCommentingEnabled = true,
        string? activityConfigJson = null
    )
    {
        return string.IsNullOrWhiteSpace(name)
                ? Result.Failure<LearningActivity>(
                    LmsErrors.Validation(
                        "Activity.NameRequired",
                        "LearningActivity name is required."
                    )
                )
            : points < 0
                ? Result.Failure<LearningActivity>(
                    LmsErrors.Validation(
                        "Activity.PointsInvalid",
                        "Activity points cannot be negative."
                    )
                )
            : Result.Success(
                new LearningActivity(LearningActivityId.Create())
                {
                    LearningClassId = classId,
                    Name = name.Trim(),
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    ComponentType = componentType,
                    AssignTo = assignTo,
                    Order = order,
                    Points = points,
                    AvailabilityCriteria = availabilityCriteria,
                    AvailableDateDefault = availableDateDefault?.ToUniversalTime(),
                    AvailableDateOffsetDays = availableDateOffsetDays,
                    DueDateCriteria = dueDateCriteria,
                    DueDateDefault = dueDateDefault?.ToUniversalTime(),
                    DueDateOffsetDays = dueDateOffsetDays,
                    IsStudentCommentingEnabled = isStudentCommentingEnabled,
                    ActivityConfigJson = string.IsNullOrWhiteSpace(activityConfigJson)
                        ? null
                        : activityConfigJson.Trim(),
                }
            );
    }

    public DateTimeOffset? CalculateDueDateUtc(DateTimeOffset enrolledUtc, DateOnly? termStartDate)
    {
        return DueDateCriteria switch
        {
            ActivityDueDateCriteria.NoDate => null,
            ActivityDueDateCriteria.SpecificDate => DueDateDefault,
            ActivityDueDateCriteria.EnrollmentOffset => enrolledUtc
                .ToUniversalTime()
                .AddDays(DueDateOffsetDays ?? 0),
            ActivityDueDateCriteria.ClassStartOffset when termStartDate.HasValue =>
                new DateTimeOffset(
                    termStartDate.Value.ToDateTime(TimeOnly.MaxValue),
                    TimeSpan.Zero
                ).AddDays(DueDateOffsetDays ?? 0),
            ActivityDueDateCriteria.ClassStartOffset => null,
            _ => null,
        };
    }
}
