using Ruth.Modules.Workflow.Contracts.Definitions;

namespace Ruth.Host.Components.Pages.Workflow;

internal static class WorkflowDesignerSpecFactory
{
    public static WorkflowTypeSpec BuildDefaultIntakeSpec(
        string typeName,
        string typeSlug,
        string workTerm,
        string prefix
    )
    {
        return new WorkflowTypeSpec(
            typeName,
            typeSlug,
            workTerm,
            prefix,
            [BuildIntakeActivity(), BuildApprovalActivity()],
            Attributes:
            [
                new WorkflowAttributeSpec("RequestKind", "Request Kind", "select", "General"),
            ]
        );
    }

    private static WorkflowActivityTypeSpec BuildIntakeActivity()
    {
        return new WorkflowActivityTypeSpec(
            "intake-form",
            "Initial Intake & Triage",
            Order: 0,
            IsActivatedWithWorkflow: true,
            Actions:
            [
                new WorkflowActionTypeSpec(
                    "stamp-intake",
                    "Stamp Intake Timestamp",
                    "workflow.attributes.set-value",
                    Order: 0,
                    Configuration: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["AttributeKey"] = "IntakeStartedUtc",
                        ["UseUtcNow"] = "true",
                    }
                ),
                new WorkflowActionTypeSpec(
                    "triage-entry",
                    "Coordinator Review Form",
                    "workflow.forms.user-entry",
                    Order: 1
                ),
            ]
        );
    }

    private static WorkflowActivityTypeSpec BuildApprovalActivity()
    {
        return new WorkflowActivityTypeSpec(
            "final-approval",
            "Final Resolution",
            Order: 1,
            Actions:
            [
                new WorkflowActionTypeSpec(
                    "mark-approved",
                    "Complete Approved Workflow",
                    "workflow.control.set-status",
                    Order: 0,
                    Configuration: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["StateLabel"] = "Approved",
                        ["CompleteWorkflow"] = "true",
                    }
                ),
            ]
        );
    }
}
