using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Programs;

/// <summary>Creates a LearningTerm under a LearningProgram.</summary>
/// <param name="LearningProgramId">Program identifier.</param>
/// <param name="Name">Term name (e.g., Spring 2027).</param>
/// <param name="PublicName">Optional public name.</param>
/// <param name="StartDate">Optional start date.</param>
/// <param name="EndDate">Optional end date.</param>
/// <param name="EnrollmentCloseDate">Optional enrollment close date.</param>
public sealed record CreateLearningTermCommand(
    Guid LearningProgramId,
    string Name,
    string? PublicName = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    DateOnly? EnrollmentCloseDate = null
) : ICommand<LearningTermResponse>;
