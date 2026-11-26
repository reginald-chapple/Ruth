namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Read projection of an audit log entry on a Workflow instance.</summary>
/// <param name="Id">Log entry identifier.</param>
/// <param name="EntryKind">Classification of the log entry.</param>
/// <param name="Summary">Human-readable log message.</param>
/// <param name="ActorPersonId">Optional actor CRM Person identifier.</param>
/// <param name="LoggedAtUtc">UTC timestamp when recorded.</param>
public sealed record WorkflowLogResponse(
    Guid Id,
    WorkflowLogKind EntryKind,
    string Summary,
    Guid? ActorPersonId,
    DateTimeOffset LoggedAtUtc
);
