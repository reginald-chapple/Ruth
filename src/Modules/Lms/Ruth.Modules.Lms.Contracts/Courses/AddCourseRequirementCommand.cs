using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Courses;

/// <summary>Adds a prerequisite, corequisite, or equivalent requirement to a LearningCourse.</summary>
/// <param name="LearningCourseId">Target course identifier.</param>
/// <param name="RequiredLearningCourseId">Required course identifier.</param>
/// <param name="RequirementType">Prerequisite, Corequisite, or Equivalent.</param>
public sealed record AddCourseRequirementCommand(
    Guid LearningCourseId,
    Guid RequiredLearningCourseId,
    CourseRequirementType RequirementType = CourseRequirementType.Prerequisite
) : ICommand<LearningCourseResponse>;
