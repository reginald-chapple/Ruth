using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Dispatches an integration or entity stimulus against all matching active WorkflowTriggers.</summary>
/// <param name="TriggerKind">Stimulus kind.</param>
/// <param name="EventTypeKey">Canonical stimulus key.</param>
/// <param name="EntityTypeQualifier">Optional entity qualifier.</param>
/// <param name="PrimaryEntityTypeKey">Optional linked entity type key.</param>
/// <param name="PrimaryEntityId">Optional linked entity identifier.</param>
/// <param name="InitiatorPersonId">Optional initiator CRM Person identifier.</param>
/// <param name="PayloadAttributes">Flattened payload attributes seeded onto launched workflows.</param>
public sealed record DispatchWorkflowAutomationEventCommand(
    WorkflowTriggerKind TriggerKind,
    string EventTypeKey,
    string? EntityTypeQualifier = null,
    string? PrimaryEntityTypeKey = null,
    Guid? PrimaryEntityId = null,
    Guid? InitiatorPersonId = null,
    IReadOnlyDictionary<string, string?>? PayloadAttributes = null
) : ICommand<WorkflowAutomationDispatchResponse>;
