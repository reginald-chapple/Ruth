namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Summary row in the staff MyTasks inbox representing an open interactive WorkflowActivity.</summary>
/// <param name="WorkflowId">Owning Workflow identifier.</param>
/// <param name="WorkflowTypeId">Owning WorkflowType identifier.</param>
/// <param name="WorkflowTypeName">WorkflowType display name.</param>
/// <param name="WorkflowNumber">Formatted workflow number.</param>
/// <param name="WorkflowName">Workflow instance title.</param>
/// <param name="StateLabel">Current state label.</param>
/// <param name="ActivityId">Waiting WorkflowActivity identifier.</param>
/// <param name="ActivityName">Waiting activity display name.</param>
/// <param name="ActionId">Waiting WorkflowAction identifier.</param>
/// <param name="ActionName">Waiting form action display name.</param>
/// <param name="AssignedPersonId">Optional person assignee.</param>
/// <param name="AssignedGroupId">Optional group assignee.</param>
/// <param name="ActivatedAtUtc">When this task became active.</param>
public sealed record WorkflowTaskItemResponse(
    Guid WorkflowId,
    Guid WorkflowTypeId,
    string WorkflowTypeName,
    string WorkflowNumber,
    string WorkflowName,
    string StateLabel,
    Guid ActivityId,
    string ActivityName,
    Guid ActionId,
    string ActionName,
    Guid? AssignedPersonId,
    Guid? AssignedGroupId,
    DateTimeOffset ActivatedAtUtc
);
