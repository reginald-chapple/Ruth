using Ruth.Modules.Workflow.Contracts.Instances;

namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Outcome returned after submitting an interactive WorkflowEntry form.</summary>
/// <param name="Workflow">Updated Workflow instance snapshot after post-submit execution.</param>
/// <param name="SelectedButtonKey">Executed button key.</param>
/// <param name="ConfirmationTitle">Resolved confirmation title.</param>
/// <param name="ConfirmationMessage">Merge-rendered confirmation message.</param>
/// <param name="RedirectUrl">Optional completion redirect URL.</param>
public sealed record WorkflowFormSubmissionResponse(
    WorkflowInstanceResponse Workflow,
    string SelectedButtonKey,
    string ConfirmationTitle,
    string ConfirmationMessage,
    string? RedirectUrl
);
