using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Launches a new Workflow instance from a WorkflowType UUID or slug.</summary>
/// <param name="WorkflowTypeIdOrSlug">Target WorkflowType UUID or slug.</param>
/// <param name="Name">Optional instance title override.</param>
/// <param name="InitiatorPersonId">Optional initiator CRM Person identifier.</param>
/// <param name="PrimaryEntityTypeKey">Optional linked entity type key.</param>
/// <param name="PrimaryEntityId">Optional linked entity identifier.</param>
/// <param name="InitialAttributes">Optional initial workflow attribute key/values.</param>
/// <param name="ProcessImmediately">When true, executes eligible initial actions synchronously before returning.</param>
public sealed record LaunchWorkflowCommand(
    string WorkflowTypeIdOrSlug,
    string? Name = null,
    Guid? InitiatorPersonId = null,
    string? PrimaryEntityTypeKey = null,
    Guid? PrimaryEntityId = null,
    IReadOnlyDictionary<string, string?>? InitialAttributes = null,
    bool ProcessImmediately = true
) : ICommand<WorkflowInstanceResponse>;
