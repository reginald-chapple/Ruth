namespace Ruth.Modules.Lms.Contracts.Courses;

/// <summary>Persisted LearningCourse with prerequisite requirements.</summary>
/// <param name="Id">Course identifier.</param>
/// <param name="LearningProgramId">Parent program identifier.</param>
/// <param name="Name">Internal course name.</param>
/// <param name="PublicName">Public catalog title.</param>
/// <param name="CourseCode">Short course code (e.g., LEAD-101).</param>
/// <param name="Summary">Optional summary.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Credits">Course credits or CEUs.</param>
/// <param name="MaxStudents">Optional default max students.</param>
/// <param name="AllowHistoricalAccess">Whether completed learners retain content access.</param>
/// <param name="EnableAnnouncements">Whether announcements are enabled.</param>
/// <param name="IsPublic">Whether listed in the Public Course Catalog.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="Order">Catalog display order.</param>
/// <param name="Requirements">Prerequisite and corequisite requirements.</param>
public sealed record LearningCourseResponse(
    Guid Id,
    Guid LearningProgramId,
    string Name,
    string PublicName,
    string CourseCode,
    string? Summary,
    string? Description,
    int Credits,
    int? MaxStudents,
    bool AllowHistoricalAccess,
    bool EnableAnnouncements,
    bool IsPublic,
    bool IsActive,
    int Order,
    IReadOnlyList<LearningCourseRequirementResponse> Requirements
);
