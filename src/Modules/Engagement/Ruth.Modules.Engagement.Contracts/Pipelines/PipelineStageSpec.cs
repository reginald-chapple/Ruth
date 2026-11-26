namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Input specification for a stage within a PipelineType.</summary>
/// <param name="Name">Stage display name.</param>
/// <param name="Order">Zero-based stage order.</param>
/// <param name="Id">Optional existing stage ID when updating.</param>
/// <param name="Description">Optional stage description.</param>
/// <param name="IsInitial">Whether new requests enter at this stage.</param>
/// <param name="IsTerminalCompleted">Whether reaching this stage completes/connects the request.</param>
/// <param name="IsTerminalInactive">Whether reaching this stage marks the request inactive.</param>
/// <param name="IdleThresholdDaysOverride">Optional per-stage idle threshold override in days.</param>
/// <param name="Automations">Optional declarative stage automations.</param>
public sealed record PipelineStageSpec(
    string Name,
    int Order,
    Guid? Id = null,
    string? Description = null,
    bool IsInitial = false,
    bool IsTerminalCompleted = false,
    bool IsTerminalInactive = false,
    int? IdleThresholdDaysOverride = null,
    IReadOnlyList<StageAutomationDto>? Automations = null
);
