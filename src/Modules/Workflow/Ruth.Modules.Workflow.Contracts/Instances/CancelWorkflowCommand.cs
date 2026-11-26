using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Cancels an open Workflow instance.</summary>
/// <param name="WorkflowId">Target Workflow identifier.</param>
/// <param name="Reason">Cancellation reason recorded in the audit log.</param>
/// <param name="ActorPersonId">Optional actor CRM Person identifier.</param>
public sealed record CancelWorkflowCommand(
    Guid WorkflowId,
    string Reason,
    Guid? ActorPersonId = null
) : ICommand<WorkflowInstanceResponse>;
