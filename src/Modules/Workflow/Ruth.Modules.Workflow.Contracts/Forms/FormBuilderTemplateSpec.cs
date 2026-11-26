namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Specification for creating or updating a reusable FormBuilderTemplate.</summary>
/// <param name="Name">Template display name.</param>
/// <param name="Slug">Unique URL-safe slug.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="FormHeaderHtml">Optional shared header markup.</param>
/// <param name="FormFooterHtml">Optional shared footer markup.</param>
/// <param name="ConfirmationTitle">Heading displayed after form submission.</param>
/// <param name="ConfirmationMessageTemplate">Merge-enabled confirmation body.</param>
/// <param name="IncludePersonEntrySection">Whether to capture submitter name/email automatically.</param>
/// <param name="AllowNotes">Whether notes are enabled on submissions.</param>
/// <param name="CompletionRedirectUrl">Optional post-submit redirect URL.</param>
/// <param name="LinkedWorkflowTypeId">Optional default WorkflowType launched by this template.</param>
public sealed record FormBuilderTemplateSpec(
    string Name,
    string Slug,
    string? Description = null,
    bool IsActive = true,
    string? FormHeaderHtml = null,
    string? FormFooterHtml = null,
    string ConfirmationTitle = "Submission Received",
    string ConfirmationMessageTemplate =
        "Thank you. Reference {{ WorkflowNumber }} has been recorded.",
    bool IncludePersonEntrySection = true,
    bool AllowNotes = false,
    string? CompletionRedirectUrl = null,
    Guid? LinkedWorkflowTypeId = null
);
