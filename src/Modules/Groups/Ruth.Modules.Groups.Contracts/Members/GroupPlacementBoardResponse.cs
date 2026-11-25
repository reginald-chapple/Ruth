namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>GroupPlacement board projection showing source candidates and destination groups.</summary>
public sealed record GroupPlacementBoardResponse(
    Guid? SourceGroupId,
    IReadOnlyList<GroupMemberResponse> SourceMembers,
    IReadOnlyList<GroupPlacementDestinationColumnResponse> DestinationGroups
);
