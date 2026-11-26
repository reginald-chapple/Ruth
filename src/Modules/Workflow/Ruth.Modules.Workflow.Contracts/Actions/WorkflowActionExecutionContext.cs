namespace Ruth.Modules.Workflow.Contracts.Actions;

/// <summary>Read-only runtime context supplied to an IWorkflowAction plugin during execution.</summary>
/// <param name="WorkflowId">Executing Workflow instance identifier.</param>
/// <param name="WorkflowTypeId">Owning WorkflowType identifier.</param>
/// <param name="WorkflowNumber">Formatted workflow number.</param>
/// <param name="WorkflowName">Workflow instance title.</param>
/// <param name="ActivityId">Executing WorkflowActivity identifier.</param>
/// <param name="ActivityKey">Executing WorkflowActivityType key.</param>
/// <param name="ActionId">Executing WorkflowAction identifier.</param>
/// <param name="ActionTypeId">Executing WorkflowActionType identifier.</param>
/// <param name="ActionKey">Executing WorkflowActionType key.</param>
/// <param name="StateLabel">Current workflow state label.</param>
/// <param name="InitiatorPersonId">Optional initiator CRM Person identifier.</param>
/// <param name="AssignedPersonId">Optional current activity person assignee.</param>
/// <param name="AssignedGroupId">Optional current activity group assignee.</param>
/// <param name="PrimaryEntityTypeKey">Optional linked entity type key.</param>
/// <param name="PrimaryEntityId">Optional linked entity identifier.</param>
/// <param name="Configuration">Action configuration dictionary.</param>
/// <param name="WorkflowAttributes">Current workflow attributes snapshot.</param>
/// <param name="ActivityAttributes">Current activity attributes snapshot.</param>
/// <param name="NowUtc">Current UTC clock timestamp.</param>
public sealed record WorkflowActionExecutionContext(
    Guid WorkflowId,
    Guid WorkflowTypeId,
    string WorkflowNumber,
    string WorkflowName,
    Guid ActivityId,
    string ActivityKey,
    Guid ActionId,
    Guid ActionTypeId,
    string ActionKey,
    string StateLabel,
    Guid? InitiatorPersonId,
    Guid? AssignedPersonId,
    Guid? AssignedGroupId,
    string? PrimaryEntityTypeKey,
    Guid? PrimaryEntityId,
    IReadOnlyDictionary<string, string?> Configuration,
    IReadOnlyDictionary<string, string?> WorkflowAttributes,
    IReadOnlyDictionary<string, string?> ActivityAttributes,
    DateTimeOffset NowUtc
);
