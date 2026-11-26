namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Specification for creating or updating an automated WorkflowTrigger.</summary>
/// <param name="Name">Human-readable trigger name.</param>
/// <param name="TriggerKind">IntegrationEvent, EntityChange, or Scheduled.</param>
/// <param name="EventTypeKey">Canonical event or entity stimulus key (e.g., engagement.request.connected).</param>
/// <param name="WorkflowTypeId">Target WorkflowType launched when matched.</param>
/// <param name="EntityTypeQualifier">Optional entity subtype/category qualifier filter.</param>
/// <param name="FilterAttributeKey">Optional payload attribute key to inspect.</param>
/// <param name="FilterOperator">Comparison operator.</param>
/// <param name="FilterExpectedValue">Expected comparison value when applicable.</param>
/// <param name="WorkflowNameTemplate">Optional merge-friendly workflow title template.</param>
/// <param name="IsActive">Whether this trigger is enabled.</param>
/// <param name="Order">Evaluation priority order.</param>
public sealed record WorkflowTriggerSpec(
    string Name,
    WorkflowTriggerKind TriggerKind,
    string EventTypeKey,
    Guid WorkflowTypeId,
    string? EntityTypeQualifier = null,
    string? FilterAttributeKey = null,
    WorkflowTriggerOperator FilterOperator = WorkflowTriggerOperator.Always,
    string? FilterExpectedValue = null,
    string? WorkflowNameTemplate = null,
    bool IsActive = true,
    int Order = 0
);
