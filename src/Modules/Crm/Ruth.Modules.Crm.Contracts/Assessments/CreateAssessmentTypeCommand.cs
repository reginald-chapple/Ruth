using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Assessments;

/// <summary>Command to create a new assessment type definition.</summary>
public sealed record CreateAssessmentTypeCommand(
    string Key,
    string Title,
    string AssessmentPath,
    string? Description = null,
    string? AssessmentResultsPath = null,
    bool RequiresRequest = false,
    int MinimumDaysToRetake = 0,
    int ValidDurationDays = 0,
    string? IconName = null,
    string? BadgeColor = null
) : ICommand<AssessmentTypeResponse>;
