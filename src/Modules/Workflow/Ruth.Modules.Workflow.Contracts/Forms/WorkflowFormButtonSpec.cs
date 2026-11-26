namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Outcome action button rendered at the bottom of an interactive WorkflowActionForm.</summary>
/// <param name="ButtonKey">Unique button identifier (e.g., approve, deny, submit).</param>
/// <param name="Label">Button caption shown to the user.</param>
/// <param name="ButtonStyle">UI button style hint (Primary, Outline, Subtle).</param>
/// <param name="TargetActivityKey">Optional WorkflowActivityType key activated when clicked.</param>
/// <param name="CompleteCurrentActivity">Whether clicking completes the enclosing activity.</param>
/// <param name="CancelWorkflow">Whether clicking cancels the workflow instance.</param>
/// <param name="SetStateLabel">Optional workflow state label applied when clicked.</param>
/// <param name="Order">Display order.</param>
public sealed record WorkflowFormButtonSpec(
    string ButtonKey,
    string Label,
    string ButtonStyle = "Primary",
    string? TargetActivityKey = null,
    bool CompleteCurrentActivity = true,
    bool CancelWorkflow = false,
    string? SetStateLabel = null,
    int Order = 0
);
