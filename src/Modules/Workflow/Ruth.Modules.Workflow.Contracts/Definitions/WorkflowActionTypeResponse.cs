namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Read projection of an action definition within a WorkflowActivityType.</summary>
/// <param name="Id">Action type identifier.</param>
/// <param name="Key">Stable step key within the activity.</param>
/// <param name="Name">Display title.</param>
/// <param name="ActionPluginKey">Registered IWorkflowAction plugin key.</param>
/// <param name="Order">Execution order.</param>
/// <param name="IsActivityCompletedOnSuccess">Completes parent activity upon success.</param>
/// <param name="IsWorkflowCompletedOnSuccess">Completes workflow upon success.</param>
/// <param name="HasInteractiveForm">True when a W5-S6 WorkflowActionForm is attached.</param>
/// <param name="Configuration">Configured plugin parameters.</param>
public sealed record WorkflowActionTypeResponse(
    Guid Id,
    string Key,
    string Name,
    string ActionPluginKey,
    int Order,
    bool IsActivityCompletedOnSuccess,
    bool IsWorkflowCompletedOnSuccess,
    bool HasInteractiveForm,
    IReadOnlyDictionary<string, string?> Configuration
);
