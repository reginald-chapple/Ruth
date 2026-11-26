using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain.Courses;

/// <summary>Prerequisite, corequisite, or equivalent course requirement link.</summary>
public sealed class LearningCourseRequirement : Entity<LearningCourseRequirementId>
{
    private LearningCourseRequirement() { }

    public LearningCourseRequirement(
        LearningCourseId learningCourseId,
        LearningCourseId requiredLearningCourseId,
        CourseRequirementType requirementType
    )
        : base(LearningCourseRequirementId.Create())
    {
        LearningCourseId = learningCourseId;
        RequiredLearningCourseId = requiredLearningCourseId;
        RequirementType = requirementType;
    }

    public LearningCourseId LearningCourseId { get; private set; }
    public LearningCourseId RequiredLearningCourseId { get; private set; }
    public CourseRequirementType RequirementType { get; private set; }
}
