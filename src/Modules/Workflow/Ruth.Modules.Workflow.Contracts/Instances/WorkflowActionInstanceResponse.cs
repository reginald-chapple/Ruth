namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Read projection of a runtime WorkflowAction instance.</summary>
/// <param name="Id">Action instance identifier.</param>
/// <param name="WorkflowActionTypeId">Definition identifier.</param>
/// <param name="Key">Action step key.</param>
/// <param name="Name">Action step display name.</param>
/// <param name="ActionPluginKey">Executed plugin key.</param>
/// <param name="Order">Step order.</param>
/// <param name="FormActionSelected">Button key selected when completed via interactive form.</param>
/// <param name="IsCompleted">True once completed.</param>
/// <param name="LastProcessedAtUtc">Last execution UTC timestamp.</param>
/// <param name="CompletedAtUtc">Completion UTC timestamp.</param>
public sealed record WorkflowActionInstanceResponse(
    Guid Id,
    Guid WorkflowActionTypeId,
    string Key,
    string Name,
    string ActionPluginKey,
    int Order,
    string? FormActionSelected,
    bool IsCompleted,
    DateTimeOffset? LastProcessedAtUtc,
    DateTimeOffset? CompletedAtUtc
);
