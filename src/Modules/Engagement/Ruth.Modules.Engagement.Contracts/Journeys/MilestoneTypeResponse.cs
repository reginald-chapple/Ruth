namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Represents a MilestoneType configured in a JourneyProgram.</summary>
/// <param name="Id">MilestoneType identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Code">Milestone code.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IconCssClass">Optional icon class.</param>
/// <param name="Order">Display order.</param>
/// <param name="IsRequiredForProgramCompletion">Whether required for program completion.</param>
/// <param name="AllowMultipleCompletions">Whether repeatable.</param>
/// <param name="HasEndDate">Whether spans start to end dates.</param>
/// <param name="PrerequisiteMilestoneTypeIds">Resolved prerequisite MilestoneType IDs.</param>
/// <param name="PrerequisiteCodes">Prerequisite milestone codes.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record MilestoneTypeResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? IconCssClass,
    int Order,
    bool IsRequiredForProgramCompletion,
    bool AllowMultipleCompletions,
    bool HasEndDate,
    IReadOnlyList<Guid> PrerequisiteMilestoneTypeIds,
    IReadOnlyList<string> PrerequisiteCodes,
    bool IsActive
);
