using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Lists Workflow instances filtered by optional type, status, or initiator.</summary>
/// <param name="WorkflowTypeId">Optional WorkflowType filter.</param>
/// <param name="Status">Optional lifecycle status filter.</param>
/// <param name="InitiatorPersonId">Optional initiator filter.</param>
/// <param name="Limit">Maximum rows returned.</param>
public sealed record GetWorkflowInstancesQuery(
    Guid? WorkflowTypeId = null,
    WorkflowInstanceStatus? Status = null,
    Guid? InitiatorPersonId = null,
    int Limit = 50
) : IQuery<IReadOnlyList<WorkflowInstanceResponse>>;
