using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Workflow.Domain.Instances;

/// <summary>Domain event raised when a WorkflowInstance reaches Completed status.</summary>
public sealed record WorkflowCompletedDomainEvent(
    Guid WorkflowId,
    Guid WorkflowTypeId,
    string WorkflowNumber,
    string WorkflowName,
    string FinalStateLabel,
    Guid? InitiatorPersonId,
    string? PrimaryEntityTypeKey,
    Guid? PrimaryEntityId,
    DateTimeOffset CompletedAtUtc
) : DomainEvent(CompletedAtUtc);
