using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Creates or updates the interactive WorkflowActionForm bound to a WorkflowActionType.</summary>
/// <param name="WorkflowActionTypeId">Target WorkflowActionType identifier.</param>
/// <param name="Spec">Interactive form specification.</param>
public sealed record UpsertWorkflowActionFormCommand(
    Guid WorkflowActionTypeId,
    WorkflowActionFormSpec Spec
) : ICommand<WorkflowActionFormResponse>;
