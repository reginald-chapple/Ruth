namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Read projection of a WorkflowType state-machine definition.</summary>
/// <param name="Id">Workflow type identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Slug">Unique URL-safe slug.</param>
/// <param name="WorkTerm">Singular item noun.</param>
/// <param name="WorkflowIdPrefix">Prefix for generated workflow numbers.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional Core Category identifier.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="IsPersisted">Whether persisted.</param>
/// <param name="LoggingLevel">Configured logging level.</param>
/// <param name="ProcessingIntervalSeconds">Minimum background worker interval.</param>
/// <param name="MaxProcessingAttempts">Maximum retry attempts.</param>
/// <param name="Attributes">Declared workflow attribute specifications.</param>
/// <param name="Activities">Ordered activity definitions.</param>
/// <param name="CreatedAtUtc">Creation UTC timestamp.</param>
public sealed record WorkflowTypeResponse(
    Guid Id,
    string Name,
    string Slug,
    string WorkTerm,
    string WorkflowIdPrefix,
    string? Description,
    Guid? CategoryId,
    bool IsActive,
    bool IsPersisted,
    WorkflowLoggingLevel LoggingLevel,
    int ProcessingIntervalSeconds,
    int MaxProcessingAttempts,
    IReadOnlyList<WorkflowAttributeSpec> Attributes,
    IReadOnlyList<WorkflowActivityTypeResponse> Activities,
    DateTimeOffset CreatedAtUtc
);
