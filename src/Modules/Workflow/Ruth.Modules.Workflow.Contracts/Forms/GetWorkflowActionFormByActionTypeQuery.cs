using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Loads the WorkflowActionForm bound to a WorkflowActionType.</summary>
/// <param name="WorkflowActionTypeId">Target WorkflowActionType identifier.</param>
public sealed record GetWorkflowActionFormByActionTypeQuery(Guid WorkflowActionTypeId)
    : IQuery<WorkflowActionFormResponse>;
