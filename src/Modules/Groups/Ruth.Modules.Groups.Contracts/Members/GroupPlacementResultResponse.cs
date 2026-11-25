namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Summary of a GroupPlacement batch execution.</summary>
public sealed record GroupPlacementResultResponse(
    int PlacedCount,
    int FailedCount,
    IReadOnlyList<GroupPlacementOutcomeItem> Outcomes
);
