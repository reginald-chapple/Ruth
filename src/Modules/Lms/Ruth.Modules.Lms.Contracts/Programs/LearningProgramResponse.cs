namespace Ruth.Modules.Lms.Contracts.Programs;

/// <summary>Persisted LearningProgram (On-Demand vs. Academic Term/Cohort).</summary>
/// <param name="Id">Program identifier.</param>
/// <param name="Name">Internal name.</param>
/// <param name="PublicName">Public catalog display name.</param>
/// <param name="Summary">Short catalog summary.</param>
/// <param name="Description">Detailed description.</param>
/// <param name="ConfigurationMode">OnDemandLearning or AcademicCalendar.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="DefaultGradingSystemId">Optional default grading system.</param>
/// <param name="HighlightColor">Optional highlight color.</param>
/// <param name="IconCssClass">Optional icon identifier.</param>
/// <param name="IsPublic">Whether visible in the Public Course Catalog.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="IsCompletionStatusTracked">Whether Pass/Fail completion tracking is enabled.</param>
public sealed record LearningProgramResponse(
    Guid Id,
    string Name,
    string PublicName,
    string? Summary,
    string? Description,
    LearningProgramConfigurationMode ConfigurationMode,
    Guid? CategoryId,
    Guid? DefaultGradingSystemId,
    string? HighlightColor,
    string? IconCssClass,
    bool IsPublic,
    bool IsActive,
    bool IsCompletionStatusTracked
);
