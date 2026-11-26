using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Updates an existing automated WorkflowTrigger.</summary>
/// <param name="TriggerId">Target trigger identifier.</param>
/// <param name="Spec">Updated trigger specification.</param>
public sealed record UpdateWorkflowTriggerCommand(Guid TriggerId, WorkflowTriggerSpec Spec)
    : ICommand<WorkflowTriggerResponse>;
