using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Creates a new WorkflowType state-machine definition.</summary>
/// <param name="Spec">WorkflowType specification.</param>
public sealed record CreateWorkflowTypeCommand(WorkflowTypeSpec Spec)
    : ICommand<WorkflowTypeResponse>;
