namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Specification for a status option within a JourneyProgram.</summary>
/// <param name="Name">Status display name (e.g., In Progress, Completed).</param>
/// <param name="ColorHex">Badge hex color.</param>
/// <param name="IsCompleteStatus">Whether this status counts as completed.</param>
/// <param name="Order">Display order.</param>
/// <param name="Id">Optional existing status ID when updating.</param>
public sealed record MilestoneStatusSpec(
    string Name,
    string ColorHex,
    bool IsCompleteStatus,
    int Order,
    Guid? Id = null
);
