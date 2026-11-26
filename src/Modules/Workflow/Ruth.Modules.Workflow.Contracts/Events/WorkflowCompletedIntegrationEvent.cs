using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Workflow.Contracts.Events;

/// <summary>Outbox integration event emitted when a Workflow instance reaches terminal Completed status.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when completed.</param>
/// <param name="WorkflowId">Completed Workflow instance identifier.</param>
/// <param name="WorkflowTypeId">Owning WorkflowType identifier.</param>
/// <param name="WorkflowNumber">Formatted workflow number.</param>
/// <param name="WorkflowName">Instance title.</param>
/// <param name="FinalStateLabel">Terminal state label.</param>
/// <param name="InitiatorPersonId">Optional initiator CRM Person identifier.</param>
/// <param name="PrimaryEntityTypeKey">Optional linked entity type key.</param>
/// <param name="PrimaryEntityId">Optional linked entity identifier.</param>
public sealed record WorkflowCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid WorkflowId,
    Guid WorkflowTypeId,
    string WorkflowNumber,
    string WorkflowName,
    string FinalStateLabel,
    Guid? InitiatorPersonId,
    string? PrimaryEntityTypeKey,
    Guid? PrimaryEntityId
) : IIntegrationEvent;
