namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Read projection of an automated WorkflowTrigger rule.</summary>
/// <param name="Id">Trigger identifier.</param>
/// <param name="Name">Trigger display name.</param>
/// <param name="TriggerKind">Stimulus category.</param>
/// <param name="EventTypeKey">Canonical event or entity stimulus key.</param>
/// <param name="WorkflowTypeId">Target WorkflowType identifier.</param>
/// <param name="WorkflowTypeName">Target WorkflowType display name.</param>
/// <param name="EntityTypeQualifier">Optional entity subtype/qualifier.</param>
/// <param name="FilterAttributeKey">Optional inspected payload attribute key.</param>
/// <param name="FilterOperator">Predicate operator.</param>
/// <param name="FilterExpectedValue">Expected comparison value.</param>
/// <param name="WorkflowNameTemplate">Optional instance title template.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="Order">Evaluation order.</param>
public sealed record WorkflowTriggerResponse(
    Guid Id,
    string Name,
    WorkflowTriggerKind TriggerKind,
    string EventTypeKey,
    Guid WorkflowTypeId,
    string WorkflowTypeName,
    string? EntityTypeQualifier,
    string? FilterAttributeKey,
    WorkflowTriggerOperator FilterOperator,
    string? FilterExpectedValue,
    string? WorkflowNameTemplate,
    bool IsActive,
    int Order
);
