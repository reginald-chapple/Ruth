using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Reassigns an active WorkflowActivity to a person and/or group.</summary>
/// <param name="WorkflowId">Owning Workflow identifier.</param>
/// <param name="ActivityId">Target WorkflowActivity identifier.</param>
/// <param name="AssignedPersonId">Optional person assignee.</param>
/// <param name="AssignedGroupId">Optional group assignee.</param>
/// <param name="ActorPersonId">Optional actor performing the assignment.</param>
public sealed record AssignWorkflowActivityCommand(
    Guid WorkflowId,
    Guid ActivityId,
    Guid? AssignedPersonId = null,
    Guid? AssignedGroupId = null,
    Guid? ActorPersonId = null
) : ICommand<WorkflowInstanceResponse>;
