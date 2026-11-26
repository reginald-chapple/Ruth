namespace Ruth.Modules.Lms.Contracts.Courses;

/// <summary>Prerequisite/corequisite requirement on a LearningCourse.</summary>
/// <param name="Id">Requirement identifier.</param>
/// <param name="RequiredLearningCourseId">Required course identifier.</param>
/// <param name="RequiredCourseName">Required course display name.</param>
/// <param name="RequirementType">Prerequisite, Corequisite, or Equivalent.</param>
public sealed record LearningCourseRequirementResponse(
    Guid Id,
    Guid RequiredLearningCourseId,
    string RequiredCourseName,
    CourseRequirementType RequirementType
);
