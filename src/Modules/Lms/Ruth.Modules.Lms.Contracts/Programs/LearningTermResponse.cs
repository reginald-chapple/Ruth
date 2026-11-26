namespace Ruth.Modules.Lms.Contracts.Programs;

/// <summary>Persisted LearningTerm belonging to a LearningProgram.</summary>
/// <param name="Id">Term identifier.</param>
/// <param name="LearningProgramId">Parent LearningProgram identifier.</param>
/// <param name="Name">Internal name.</param>
/// <param name="PublicName">Public name.</param>
/// <param name="StartDate">Optional term start date.</param>
/// <param name="EndDate">Optional term end date.</param>
/// <param name="EnrollmentCloseDate">Optional enrollment cutoff date.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record LearningTermResponse(
    Guid Id,
    Guid LearningProgramId,
    string Name,
    string PublicName,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? EnrollmentCloseDate,
    bool IsActive
);
