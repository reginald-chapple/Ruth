using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Creates a LearningClass instance for a LearningCourse.</summary>
/// <param name="LearningCourseId">Parent course identifier.</param>
/// <param name="Name">Class name (e.g., Fall Cohort A).</param>
/// <param name="LearningTermId">Optional term identifier.</param>
/// <param name="LearningGradingSystemId">Optional grading system override.</param>
/// <param name="BranchId">Optional branch identifier.</param>
/// <param name="IsPublic">Whether publicly enrollable.</param>
/// <param name="TakesAttendance">Whether attendance is tracked.</param>
/// <param name="AllowOverbooking">Whether enrollment may exceed MaxStudents.</param>
/// <param name="MaxStudents">Optional max seats.</param>
public sealed record CreateLearningClassCommand(
    Guid LearningCourseId,
    string Name,
    Guid? LearningTermId = null,
    Guid? LearningGradingSystemId = null,
    Guid? BranchId = null,
    bool IsPublic = true,
    bool TakesAttendance = false,
    bool AllowOverbooking = false,
    int? MaxStudents = null
) : ICommand<LearningClassResponse>;
