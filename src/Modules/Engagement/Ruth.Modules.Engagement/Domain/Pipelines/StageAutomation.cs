using Ruth.Modules.Engagement.Contracts.Pipelines;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Immutable stage automation rule serialized inside a PipelineStage.</summary>
public sealed record StageAutomation(
    StageAutomationKind AutomationKind,
    Guid? TargetEntityId = null,
    string? SystemCommunicationKey = null,
    int? AutoSetFollowUpDays = null
);
