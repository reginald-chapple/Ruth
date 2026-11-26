namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Full read projection of a Workflow state-machine instance.</summary>
/// <param name="Id">Workflow instance identifier.</param>
/// <param name="WorkflowTypeId">Owning WorkflowType identifier.</param>
/// <param name="WorkflowTypeName">Owning WorkflowType display name.</param>
/// <param name="WorkflowNumber">Formatted workflow number (e.g., WF-00001).</param>
/// <param name="Name">Instance title.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="StateLabel">Human-readable workflow state label.</param>
/// <param name="InitiatorPersonId">Optional initiator CRM Person identifier.</param>
/// <param name="PrimaryEntityTypeKey">Optional linked entity type key.</param>
/// <param name="PrimaryEntityId">Optional linked entity identifier.</param>
/// <param name="Attributes">Persisted workflow-scoped JSONB attributes.</param>
/// <param name="ActivatedAtUtc">Activation UTC timestamp.</param>
/// <param name="LastProcessedAtUtc">Optional last execution UTC timestamp.</param>
/// <param name="NextScheduledProcessAtUtc">Optional next scheduled background worker timestamp.</param>
/// <param name="CompletedAtUtc">Optional completion UTC timestamp.</param>
/// <param name="ErrorMessage">Optional fault error message.</param>
/// <param name="ProcessingAttemptCount">Number of processing passes executed.</param>
/// <param name="Activities">Ordered runtime activities.</param>
/// <param name="Logs">Chronological audit log entries.</param>
public sealed record WorkflowInstanceResponse(
    Guid Id,
    Guid WorkflowTypeId,
    string WorkflowTypeName,
    string WorkflowNumber,
    string Name,
    WorkflowInstanceStatus Status,
    string StateLabel,
    Guid? InitiatorPersonId,
    string? PrimaryEntityTypeKey,
    Guid? PrimaryEntityId,
    IReadOnlyDictionary<string, string?> Attributes,
    DateTimeOffset ActivatedAtUtc,
    DateTimeOffset? LastProcessedAtUtc,
    DateTimeOffset? NextScheduledProcessAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ErrorMessage,
    int ProcessingAttemptCount,
    IReadOnlyList<WorkflowActivityInstanceResponse> Activities,
    IReadOnlyList<WorkflowLogResponse> Logs
);
