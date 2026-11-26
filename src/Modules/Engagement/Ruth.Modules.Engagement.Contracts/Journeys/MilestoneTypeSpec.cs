namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Specification for a MilestoneType step within a JourneyProgram.</summary>
/// <param name="Name">Milestone display name.</param>
/// <param name="Code">Program-unique milestone code.</param>
/// <param name="Order">Display order.</param>
/// <param name="Id">Optional existing MilestoneType ID when updating.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IconCssClass">Optional icon class.</param>
/// <param name="IsRequiredForProgramCompletion">Whether required to earn a JourneyCompletion.</param>
/// <param name="AllowMultipleCompletions">Whether a person can complete this milestone multiple times.</param>
/// <param name="HasEndDate">Whether milestones capture a date span.</param>
/// <param name="PrerequisiteCodes">Codes of prerequisite milestones in the same program.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record MilestoneTypeSpec(
    string Name,
    string Code,
    int Order,
    Guid? Id = null,
    string? Description = null,
    string? IconCssClass = null,
    bool IsRequiredForProgramCompletion = true,
    bool AllowMultipleCompletions = false,
    bool HasEndDate = false,
    IReadOnlyList<string>? PrerequisiteCodes = null,
    bool IsActive = true
);
