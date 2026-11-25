namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Per-person outcome from a bulk GroupPlacement operation.</summary>
public sealed record GroupPlacementOutcomeItem(
    Guid PersonId,
    Guid DestinationGroupId,
    bool Succeeded,
    Guid? GroupMemberId,
    string? Warning,
    string? ErrorMessage
);
