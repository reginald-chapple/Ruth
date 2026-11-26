namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Represents a stage within a PipelineType.</summary>
/// <param name="Id">Stage identifier.</param>
/// <param name="Name">Stage name.</param>
/// <param name="Description">Optional stage description.</param>
/// <param name="Order">Stage order.</param>
/// <param name="IsInitial">Whether this is an initial intake stage.</param>
/// <param name="IsTerminalCompleted">Whether this stage auto-completes/connects requests.</param>
/// <param name="IsTerminalInactive">Whether this stage marks requests inactive.</param>
/// <param name="IdleThresholdDaysOverride">Optional per-stage idle threshold override.</param>
/// <param name="Automations">Stage automations.</param>
public sealed record PipelineStageResponse(
    Guid Id,
    string Name,
    string? Description,
    int Order,
    bool IsInitial,
    bool IsTerminalCompleted,
    bool IsTerminalInactive,
    int? IdleThresholdDaysOverride,
    IReadOnlyList<StageAutomationDto> Automations
);
