using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Executes one full processing pass on an existing Workflow instance.</summary>
/// <param name="WorkflowId">Target Workflow instance identifier.</param>
public sealed record ProcessWorkflowCommand(Guid WorkflowId) : ICommand<WorkflowInstanceResponse>;
