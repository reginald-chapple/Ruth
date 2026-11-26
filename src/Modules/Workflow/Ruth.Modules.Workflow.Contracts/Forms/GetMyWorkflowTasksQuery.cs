using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Queries open interactive WorkflowActivity tasks for the MyTasks inbox.</summary>
/// <param name="AssignedPersonId">Optional person assignee filter.</param>
/// <param name="AssignedGroupId">Optional group assignee filter.</param>
/// <param name="IncludeUnassigned">When true, includes unassigned open tasks.</param>
public sealed record GetMyWorkflowTasksQuery(
    Guid? AssignedPersonId = null,
    Guid? AssignedGroupId = null,
    bool IncludeUnassigned = true
) : IQuery<IReadOnlyList<WorkflowTaskItemResponse>>;
