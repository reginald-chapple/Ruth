namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Specification for a single ordered action step inside a WorkflowActivityType.</summary>
/// <param name="Key">Stable slug/key identifying this action step within the activity.</param>
/// <param name="Name">Human-readable action step title.</param>
/// <param name="ActionPluginKey">Registered IWorkflowAction plugin key (e.g., workflow.attributes.set-value).</param>
/// <param name="Order">Execution order within the parent activity.</param>
/// <param name="IsActivityCompletedOnSuccess">Marks the parent activity completed immediately when this action succeeds.</param>
/// <param name="IsWorkflowCompletedOnSuccess">Marks the entire workflow completed immediately when this action succeeds.</param>
/// <param name="Configuration">Declarative key/value parameters passed to the IWorkflowAction plugin.</param>
public sealed record WorkflowActionTypeSpec(
    string Key,
    string Name,
    string ActionPluginKey,
    int Order = 0,
    bool IsActivityCompletedOnSuccess = false,
    bool IsWorkflowCompletedOnSuccess = false,
    IReadOnlyDictionary<string, string?>? Configuration = null
);
