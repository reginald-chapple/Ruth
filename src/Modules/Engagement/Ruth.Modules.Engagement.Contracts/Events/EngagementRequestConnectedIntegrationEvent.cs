using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Contracts.Events;

/// <summary>Outbox integration event published when an EngagementRequest reaches Connected status.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when connected.</param>
/// <param name="EngagementRequestId">Completed engagement request identifier.</param>
/// <param name="PipelineTypeId">Owning pipeline type identifier.</param>
/// <param name="EngagementOpportunityId">Owning engagement opportunity identifier.</param>
/// <param name="RequesterPersonId">Connected constituent person identifier.</param>
/// <param name="AssignedConnectorPersonId">Optional coordinator person identifier.</param>
/// <param name="PlacementGroupId">Optional destination placement group identifier.</param>
public sealed record EngagementRequestConnectedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid EngagementRequestId,
    Guid PipelineTypeId,
    Guid EngagementOpportunityId,
    Guid RequesterPersonId,
    Guid? AssignedConnectorPersonId,
    Guid? PlacementGroupId
) : IIntegrationEvent;
