namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Declarative specification for creating or updating a WorkflowType definition.</summary>
/// <param name="Name">Display name of the workflow definition.</param>
/// <param name="Slug">Unique URL-safe slug.</param>
/// <param name="WorkTerm">Singular noun describing an item (e.g., Request, Application, Case).</param>
/// <param name="WorkflowIdPrefix">Prefix prepended to generated workflow numbers (e.g., WF-).</param>
/// <param name="Activities">Ordered activity type definitions.</param>
/// <param name="Attributes">Optional typed workflow-level attribute specifications.</param>
/// <param name="Description">Optional administrative summary.</param>
/// <param name="CategoryId">Optional Core Category identifier.</param>
/// <param name="IsActive">Whether new instances of this workflow type may be launched.</param>
/// <param name="IsPersisted">Whether instances persist automatically across steps.</param>
/// <param name="LoggingLevel">Execution audit log detail level.</param>
/// <param name="ProcessingIntervalSeconds">Minimum seconds between background worker passes.</param>
/// <param name="MaxProcessingAttempts">Maximum retry attempts before faulting.</param>
public sealed record WorkflowTypeSpec(
    string Name,
    string Slug,
    string WorkTerm,
    string WorkflowIdPrefix,
    IReadOnlyList<WorkflowActivityTypeSpec> Activities,
    IReadOnlyList<WorkflowAttributeSpec>? Attributes = null,
    string? Description = null,
    Guid? CategoryId = null,
    bool IsActive = true,
    bool IsPersisted = true,
    WorkflowLoggingLevel LoggingLevel = WorkflowLoggingLevel.Action,
    int ProcessingIntervalSeconds = 60,
    int MaxProcessingAttempts = 5
);
