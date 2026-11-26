namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Result payload returned when an EngagementRequest transitions to a new PipelineStage.</summary>
public sealed record StageTransitionOutcome(
    Guid PreviousStageId,
    Guid NewStageId,
    bool BecameConnected,
    bool BecameInactive,
    IReadOnlyList<StageAutomation> TriggeredAutomations
);
