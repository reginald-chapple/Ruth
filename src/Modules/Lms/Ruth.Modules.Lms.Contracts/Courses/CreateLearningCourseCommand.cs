using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Courses;

/// <summary>Creates a LearningCourse inside a LearningProgram.</summary>
/// <param name="LearningProgramId">Parent program identifier.</param>
/// <param name="Name">Course name.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="PublicName">Optional public name.</param>
/// <param name="Summary">Optional summary.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Credits">Credit count.</param>
/// <param name="MaxStudents">Optional max seats.</param>
/// <param name="AllowHistoricalAccess">Whether historical access is enabled.</param>
/// <param name="EnableAnnouncements">Whether announcements are enabled.</param>
/// <param name="IsPublic">Whether listed publicly.</param>
/// <param name="Order">Display order.</param>
public sealed record CreateLearningCourseCommand(
    Guid LearningProgramId,
    string Name,
    string CourseCode,
    string? PublicName = null,
    string? Summary = null,
    string? Description = null,
    int Credits = 0,
    int? MaxStudents = null,
    bool AllowHistoricalAccess = true,
    bool EnableAnnouncements = true,
    bool IsPublic = true,
    int Order = 0
) : ICommand<LearningCourseResponse>;
