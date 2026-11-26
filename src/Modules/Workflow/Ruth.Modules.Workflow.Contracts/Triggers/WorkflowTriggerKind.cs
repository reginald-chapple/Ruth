namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Source stimulus category for an automated WorkflowTrigger.</summary>
public enum WorkflowTriggerKind
{
    /// <summary>Triggered by a cross-module Outbox integration event.</summary>
    IntegrationEvent = 0,

    /// <summary>Triggered by an entity state or qualifier mutation.</summary>
    EntityChange = 1,

    /// <summary>Triggered on a scheduled automation sweep.</summary>
    Scheduled = 2,
}
