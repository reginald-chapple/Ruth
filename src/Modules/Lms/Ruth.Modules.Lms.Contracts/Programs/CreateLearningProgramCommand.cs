using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Programs;

/// <summary>Creates a LearningProgram (On-Demand or Academic Term/Cohort).</summary>
/// <param name="Name">Program name.</param>
/// <param name="PublicName">Optional public name (defaults to Name).</param>
/// <param name="ConfigurationMode">OnDemandLearning or AcademicCalendar.</param>
/// <param name="Summary">Optional summary.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="DefaultGradingSystemId">Optional default grading system.</param>
/// <param name="HighlightColor">Optional highlight color.</param>
/// <param name="IconCssClass">Optional icon class.</param>
/// <param name="IsPublic">Whether publicly discoverable.</param>
/// <param name="IsCompletionStatusTracked">Whether completion tracking is active.</param>
public sealed record CreateLearningProgramCommand(
    string Name,
    string? PublicName = null,
    LearningProgramConfigurationMode ConfigurationMode =
        LearningProgramConfigurationMode.OnDemandLearning,
    string? Summary = null,
    string? Description = null,
    Guid? CategoryId = null,
    Guid? DefaultGradingSystemId = null,
    string? HighlightColor = null,
    string? IconCssClass = null,
    bool IsPublic = true,
    bool IsCompletionStatusTracked = true
) : ICommand<LearningProgramResponse>;
