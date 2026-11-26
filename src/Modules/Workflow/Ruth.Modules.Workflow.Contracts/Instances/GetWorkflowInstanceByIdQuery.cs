using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Loads a single Workflow instance with activities, actions, and logs.</summary>
/// <param name="WorkflowId">Target Workflow identifier.</param>
public sealed record GetWorkflowInstanceByIdQuery(Guid WorkflowId)
    : IQuery<WorkflowInstanceResponse>;
