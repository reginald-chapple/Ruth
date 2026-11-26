namespace Ruth.Modules.Lms.Contracts.Courses;

/// <summary>Relationship type between a LearningCourse and a required course.</summary>
public enum CourseRequirementType
{
    /// <summary>Must be completed prior to enrollment.</summary>
    Prerequisite = 0,

    /// <summary>Must be taken concurrently or previously.</summary>
    Corequisite = 1,

    /// <summary>Recognized as an equivalent completion.</summary>
    Equivalent = 2,
}
