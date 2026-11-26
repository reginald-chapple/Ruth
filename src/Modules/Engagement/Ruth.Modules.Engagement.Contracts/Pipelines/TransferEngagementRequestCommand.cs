using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to transfer an EngagementRequest to a different EngagementOpportunity.</summary>
/// <param name="RequestId">EngagementRequest identifier.</param>
/// <param name="TargetOpportunityId">Destination EngagementOpportunity identifier.</param>
/// <param name="Note">Optional transfer reason.</param>
/// <param name="ActorPersonId">Optional actor Person identifier.</param>
public sealed record TransferEngagementRequestCommand(
    Guid RequestId,
    Guid TargetOpportunityId,
    string? Note = null,
    Guid? ActorPersonId = null
) : ICommand<EngagementRequestResponse>;
