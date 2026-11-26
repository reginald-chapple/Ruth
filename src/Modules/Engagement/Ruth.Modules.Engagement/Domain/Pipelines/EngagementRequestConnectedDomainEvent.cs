using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Domain event raised when an EngagementRequest reaches Connected status.</summary>
public sealed record EngagementRequestConnectedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid EngagementRequestId,
    Guid PipelineTypeId,
    Guid EngagementOpportunityId,
    Guid RequesterPersonId,
    Guid? PlacementGroupId
) : DomainEvent(EventId, OccurredOnUtc);
