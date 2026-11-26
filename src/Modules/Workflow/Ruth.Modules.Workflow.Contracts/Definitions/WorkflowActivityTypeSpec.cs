namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Specification for an activity stage within a WorkflowType definition.</summary>
/// <param name="Key">Stable slug/key used for control-flow routing and form button jumps.</param>
/// <param name="Name">Human-readable activity name.</param>
/// <param name="Order">Display and default evaluation order.</param>
/// <param name="IsActivatedWithWorkflow">Whether an instance of this activity activates automatically on workflow launch.</param>
/// <param name="IsActive">Whether this activity definition is enabled.</param>
/// <param name="Description">Optional coordinator/admin guidance.</param>
/// <param name="DefaultAssignedPersonId">Optional default assignee CRM Person identifier.</param>
/// <param name="DefaultAssignedGroupId">Optional default assignee Group/Team identifier.</param>
/// <param name="Actions">Ordered action definitions executed inside this activity.</param>
public sealed record WorkflowActivityTypeSpec(
    string Key,
    string Name,
    int Order = 0,
    bool IsActivatedWithWorkflow = false,
    bool IsActive = true,
    string? Description = null,
    Guid? DefaultAssignedPersonId = null,
    Guid? DefaultAssignedGroupId = null,
    IReadOnlyList<WorkflowActionTypeSpec>? Actions = null
);
