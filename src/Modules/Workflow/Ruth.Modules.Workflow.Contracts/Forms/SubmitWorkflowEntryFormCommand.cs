using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Submits an interactive WorkflowEntry form, validates visible fields, applies button routing, and resumes workflow processing.</summary>
/// <param name="WorkflowId">Target Workflow instance identifier.</param>
/// <param name="ActionId">Waiting WorkflowAction instance identifier.</param>
/// <param name="SelectedButtonKey">Chosen outcome button key.</param>
/// <param name="FieldValues">Submitted key/value field inputs.</param>
/// <param name="ActorPersonId">Optional submitting CRM Person identifier.</param>
/// <param name="SubmitterFirstName">Optional person-entry first name.</param>
/// <param name="SubmitterLastName">Optional person-entry last name.</param>
/// <param name="SubmitterEmail">Optional person-entry email.</param>
/// <param name="Note">Optional submission note.</param>
public sealed record SubmitWorkflowEntryFormCommand(
    Guid WorkflowId,
    Guid ActionId,
    string SelectedButtonKey,
    IReadOnlyDictionary<string, string?> FieldValues,
    Guid? ActorPersonId = null,
    string? SubmitterFirstName = null,
    string? SubmitterLastName = null,
    string? SubmitterEmail = null,
    string? Note = null
) : ICommand<WorkflowFormSubmissionResponse>;
