using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Places one or more people into destination groups, optionally removing them from a source group.</summary>
public sealed record PlaceGroupMembersCommand(
    IReadOnlyList<GroupPlacementAssignmentDto> Assignments,
    Guid? RemoveFromSourceGroupId = null
) : ICommand<GroupPlacementResultResponse>;
