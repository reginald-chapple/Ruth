using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Lms.Contracts.Events;

/// <summary>Published via Outbox when a public LearningCourse is created or updated for search indexing.</summary>
/// <param name="LearningCourseId">Course identifier.</param>
/// <param name="LearningProgramId">Program identifier.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="Name">Course name.</param>
/// <param name="Summary">Optional summary.</param>
/// <param name="IsPublic">Whether visible in the public catalog.</param>
public sealed record LearningCoursePublishedIntegrationEvent(
    Guid LearningCourseId,
    Guid LearningProgramId,
    string CourseCode,
    string Name,
    string? Summary,
    bool IsPublic
) : IntegrationEvent;
