namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Read projection of an activity stage within a WorkflowType.</summary>
/// <param name="Id">Activity type identifier.</param>
/// <param name="Key">Stable activity routing key.</param>
/// <param name="Name">Display name.</param>
/// <param name="Order">Sort and execution order.</param>
/// <param name="IsActivatedWithWorkflow">True when activated automatically on launch.</param>
/// <param name="IsActive">True when enabled.</param>
/// <param name="Description">Optional description.</param>
/// <param name="DefaultAssignedPersonId">Optional default person assignee.</param>
/// <param name="DefaultAssignedGroupId">Optional default group assignee.</param>
/// <param name="Actions">Ordered action definitions.</param>
public sealed record WorkflowActivityTypeResponse(
    Guid Id,
    string Key,
    string Name,
    int Order,
    bool IsActivatedWithWorkflow,
    bool IsActive,
    string? Description,
    Guid? DefaultAssignedPersonId,
    Guid? DefaultAssignedGroupId,
    IReadOnlyList<WorkflowActionTypeResponse> Actions
);
