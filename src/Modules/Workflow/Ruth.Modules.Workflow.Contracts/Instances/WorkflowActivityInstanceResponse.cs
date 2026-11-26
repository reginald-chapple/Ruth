namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Read projection of a runtime WorkflowActivity instance.</summary>
/// <param name="Id">Activity instance identifier.</param>
/// <param name="WorkflowActivityTypeId">Activity definition identifier.</param>
/// <param name="Key">Activity routing key.</param>
/// <param name="Name">Activity display name.</param>
/// <param name="Order">Activity order.</param>
/// <param name="AssignedPersonId">Optional assigned CRM Person identifier.</param>
/// <param name="AssignedGroupId">Optional assigned Group identifier.</param>
/// <param name="IsCompleted">True when completed.</param>
/// <param name="ActivatedAtUtc">UTC timestamp when activated.</param>
/// <param name="CompletedAtUtc">Optional UTC timestamp when completed.</param>
/// <param name="Attributes">Persisted activity-scoped attributes.</param>
/// <param name="Actions">Ordered action instances.</param>
public sealed record WorkflowActivityInstanceResponse(
    Guid Id,
    Guid WorkflowActivityTypeId,
    string Key,
    string Name,
    int Order,
    Guid? AssignedPersonId,
    Guid? AssignedGroupId,
    bool IsCompleted,
    DateTimeOffset ActivatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyDictionary<string, string?> Attributes,
    IReadOnlyList<WorkflowActionInstanceResponse> Actions
);
