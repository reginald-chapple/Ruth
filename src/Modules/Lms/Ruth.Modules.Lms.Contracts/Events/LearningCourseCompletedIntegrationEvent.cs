using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Lms.Contracts.Events;

/// <summary>Published via Outbox when a learner passes and completes a LearningCourse.</summary>
/// <param name="ParticipantId">LearningParticipant identifier.</param>
/// <param name="PersonId">Learner PersonId.</param>
/// <param name="LearningProgramId">Owning LearningProgram identifier.</param>
/// <param name="LearningCourseId">Completed LearningCourse identifier.</param>
/// <param name="LearningClassId">Completed LearningClass identifier.</param>
/// <param name="CourseName">Course display name.</param>
/// <param name="GradePercent">Final grade percentage.</param>
/// <param name="GradeScaleName">Awarded grade scale band label.</param>
/// <param name="CompletedOnUtc">Completion timestamp in UTC.</param>
public sealed record LearningCourseCompletedIntegrationEvent(
    Guid ParticipantId,
    Guid PersonId,
    Guid LearningProgramId,
    Guid LearningCourseId,
    Guid LearningClassId,
    string CourseName,
    decimal GradePercent,
    string? GradeScaleName,
    DateTimeOffset CompletedOnUtc
) : IntegrationEvent;
