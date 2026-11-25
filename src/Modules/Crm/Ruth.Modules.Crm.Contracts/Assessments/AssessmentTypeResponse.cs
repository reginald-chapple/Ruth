namespace Ruth.Modules.Crm.Contracts.Assessments;

/// <summary>Represents a configured assessment type (e.g., Team Strengths, DISC, Conflict, Motivators, EQ).</summary>
public sealed record AssessmentTypeResponse(
    Guid Id,
    string Key,
    string Title,
    string? Description,
    string AssessmentPath,
    string? AssessmentResultsPath,
    bool IsActive,
    bool RequiresRequest,
    int MinimumDaysToRetake,
    int ValidDurationDays,
    bool IsSystem,
    string? IconName,
    string? BadgeColor
);
