namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Read projection of a reusable FormBuilderTemplate.</summary>
/// <param name="Id">Template identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Slug">Unique slug.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="FormHeaderHtml">Shared header markup.</param>
/// <param name="FormFooterHtml">Shared footer markup.</param>
/// <param name="ConfirmationTitle">Post-submit confirmation title.</param>
/// <param name="ConfirmationMessageTemplate">Merge-enabled confirmation body.</param>
/// <param name="IncludePersonEntrySection">Whether submitter contact capture is enabled.</param>
/// <param name="AllowNotes">Whether notes are enabled.</param>
/// <param name="CompletionRedirectUrl">Optional redirect URL.</param>
/// <param name="LinkedWorkflowTypeId">Optional linked WorkflowType identifier.</param>
/// <param name="CreatedAtUtc">Creation UTC timestamp.</param>
public sealed record FormBuilderTemplateResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    string? FormHeaderHtml,
    string? FormFooterHtml,
    string ConfirmationTitle,
    string ConfirmationMessageTemplate,
    bool IncludePersonEntrySection,
    bool AllowNotes,
    string? CompletionRedirectUrl,
    Guid? LinkedWorkflowTypeId,
    DateTimeOffset CreatedAtUtc
);
