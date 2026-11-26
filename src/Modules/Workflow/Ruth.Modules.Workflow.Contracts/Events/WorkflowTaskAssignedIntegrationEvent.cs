using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Workflow.Contracts.Events;

/// <summary>Outbox integration event emitted when an interactive WorkflowActivity task is assigned or awaits user entry.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when assigned.</param>
/// <param name="WorkflowId">Owning Workflow identifier.</param>
/// <param name="ActivityId">Assigned WorkflowActivity identifier.</param>
/// <param name="WorkflowNumber">Formatted workflow number.</param>
/// <param name="ActivityName">Display name of the assigned activity.</param>
/// <param name="AssignedPersonId">Optional person assignee.</param>
/// <param name="AssignedGroupId">Optional group assignee.</param>
public sealed record WorkflowTaskAssignedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid WorkflowId,
    Guid ActivityId,
    string WorkflowNumber,
    string ActivityName,
    Guid? AssignedPersonId,
    Guid? AssignedGroupId
) : IIntegrationEvent;
