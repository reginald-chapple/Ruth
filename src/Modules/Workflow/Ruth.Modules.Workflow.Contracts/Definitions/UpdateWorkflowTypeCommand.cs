using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Updates an existing WorkflowType definition and synchronizes its activities and actions.</summary>
/// <param name="WorkflowTypeId">Target WorkflowType identifier.</param>
/// <param name="Spec">Updated WorkflowType specification.</param>
public sealed record UpdateWorkflowTypeCommand(Guid WorkflowTypeId, WorkflowTypeSpec Spec)
    : ICommand<WorkflowTypeResponse>;
