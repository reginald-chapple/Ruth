using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to mark an EngagementRequest Connected and optionally place the constituent in a Group.</summary>
/// <param name="RequestId">EngagementRequest identifier.</param>
/// <param name="PlacementGroupId">Optional destination placement Group identifier.</param>
/// <param name="PlacementGroupRoleId">Optional destination GroupRole identifier.</param>
/// <param name="ActorPersonId">Optional actor Person identifier.</param>
/// <param name="Note">Optional completion note.</param>
public sealed record ConnectEngagementRequestCommand(
    Guid RequestId,
    Guid? PlacementGroupId = null,
    Guid? PlacementGroupRoleId = null,
    Guid? ActorPersonId = null,
    string? Note = null
) : ICommand<EngagementRequestResponse>;
