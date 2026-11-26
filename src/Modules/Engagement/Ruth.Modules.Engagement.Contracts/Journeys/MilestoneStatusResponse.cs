namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Represents a MilestoneStatus within a JourneyProgram.</summary>
/// <param name="Id">MilestoneStatus identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="ColorHex">Badge hex color.</param>
/// <param name="IsCompleteStatus">Whether this status marks a milestone complete.</param>
/// <param name="Order">Display order.</param>
public sealed record MilestoneStatusResponse(
    Guid Id,
    string Name,
    string ColorHex,
    bool IsCompleteStatus,
    int Order
);
