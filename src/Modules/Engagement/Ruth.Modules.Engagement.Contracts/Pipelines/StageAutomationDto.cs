namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Declarative automation rule attached to a pipeline stage.</summary>
/// <param name="AutomationKind">Kind of automated action.</param>
/// <param name="TargetEntityId">Optional target group or opportunity identifier.</param>
/// <param name="SystemCommunicationKey">Optional system communication template key.</param>
/// <param name="AutoSetFollowUpDays">Optional future follow-up offset in days.</param>
public sealed record StageAutomationDto(
    StageAutomationKind AutomationKind,
    Guid? TargetEntityId = null,
    string? SystemCommunicationKey = null,
    int? AutoSetFollowUpDays = null
);
