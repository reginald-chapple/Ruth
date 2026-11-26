using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Creates a new automated WorkflowTrigger.</summary>
/// <param name="Spec">Trigger specification.</param>
public sealed record CreateWorkflowTriggerCommand(WorkflowTriggerSpec Spec)
    : ICommand<WorkflowTriggerResponse>;
