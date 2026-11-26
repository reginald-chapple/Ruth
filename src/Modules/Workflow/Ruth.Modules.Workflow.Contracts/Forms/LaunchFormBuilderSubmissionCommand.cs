using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Launches a FormBuilderTemplate's linked WorkflowType and optionally submits its first interactive form in one step.</summary>
/// <param name="TemplateIdOrSlug">Target FormBuilderTemplate UUID or slug.</param>
/// <param name="SelectedButtonKey">Optional button key to auto-submit on the initial entry form.</param>
/// <param name="FieldValues">Initial/submitted form field values.</param>
/// <param name="SubmitterFirstName">Optional submitter first name.</param>
/// <param name="SubmitterLastName">Optional submitter last name.</param>
/// <param name="SubmitterEmail">Optional submitter email.</param>
/// <param name="InitiatorPersonId">Optional authenticated person identifier.</param>
public sealed record LaunchFormBuilderSubmissionCommand(
    string TemplateIdOrSlug,
    string? SelectedButtonKey = null,
    IReadOnlyDictionary<string, string?>? FieldValues = null,
    string? SubmitterFirstName = null,
    string? SubmitterLastName = null,
    string? SubmitterEmail = null,
    Guid? InitiatorPersonId = null
) : ICommand<WorkflowFormSubmissionResponse>;
