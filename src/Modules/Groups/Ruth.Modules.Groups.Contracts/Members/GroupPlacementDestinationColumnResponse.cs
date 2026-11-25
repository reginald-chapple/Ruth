namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Destination group column summary for the GroupPlacement board.</summary>
public sealed record GroupPlacementDestinationColumnResponse(
    Guid GroupId,
    string GroupName,
    int? GroupCapacity,
    int ActiveMemberCount,
    int PendingMemberCount,
    IReadOnlyList<GroupMemberResponse> Members
);
