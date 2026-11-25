namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Individual placement instruction within a GroupPlacement operation.</summary>
public sealed record GroupPlacementAssignmentDto(
    Guid PersonId,
    Guid DestinationGroupId,
    Guid? DestinationRoleId = null,
    GroupMemberStatus Status = GroupMemberStatus.Active
);
