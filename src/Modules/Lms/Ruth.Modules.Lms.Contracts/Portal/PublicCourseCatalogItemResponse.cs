using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Programs;

namespace Ruth.Modules.Lms.Contracts.Portal;

/// <summary>Public catalog entry combining a course with its active public classes and optional learner status.</summary>
/// <param name="Program">Parent program.</param>
/// <param name="Course">Public course.</param>
/// <param name="OpenClasses">Active public classes available for enrollment.</param>
/// <param name="PrerequisitesMet">Whether the requesting learner satisfies all prerequisites.</param>
/// <param name="CurrentEnrollment">Current enrollment if the learner is enrolled.</param>
public sealed record PublicCourseCatalogItemResponse(
    LearningProgramResponse Program,
    LearningCourseResponse Course,
    IReadOnlyList<LearningClassResponse> OpenClasses,
    bool PrerequisitesMet,
    LearningParticipantResponse? CurrentEnrollment
);
