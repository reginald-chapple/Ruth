using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Loads the GroupPlacement board for a set of destination groups and optional source group.</summary>
public sealed record GetGroupPlacementBoardQuery(
    IReadOnlyList<Guid> DestinationGroupIds,
    Guid? SourceGroupId = null
) : IQuery<GroupPlacementBoardResponse>;
