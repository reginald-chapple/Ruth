using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Courses;

/// <summary>Lists LearningCourses optionally filtered by program or public visibility.</summary>
/// <param name="LearningProgramId">Optional program identifier.</param>
/// <param name="PublicOnly">When true, returns only public active courses.</param>
public sealed record GetLearningCoursesQuery(
    Guid? LearningProgramId = null,
    bool PublicOnly = false
) : IQuery<IReadOnlyList<LearningCourseResponse>>;
