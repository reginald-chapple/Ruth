using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Contracts.Events;

/// <summary>Outbox integration event published when an EngagementRequest transitions between pipeline stages.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp of transition.</param>
/// <param name="EngagementRequestId">Engagement request identifier.</param>
/// <param name="PipelineTypeId">Pipeline type identifier.</param>
/// <param name="PreviousStageId">Previous stage identifier.</param>
/// <param name="NewStageId">New stage identifier.</param>
/// <param name="RequesterPersonId">Requester person identifier.</param>
public sealed record EngagementRequestStageChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid EngagementRequestId,
    Guid PipelineTypeId,
    Guid PreviousStageId,
    Guid NewStageId,
    Guid RequesterPersonId
) : IIntegrationEvent;
