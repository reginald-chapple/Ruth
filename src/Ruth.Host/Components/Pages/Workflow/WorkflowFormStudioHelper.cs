using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Forms;

namespace Ruth.Host.Components.Pages.Workflow;

internal static class WorkflowFormStudioHelper
{
    public static List<ActionSelectionOption> ExtractFormCapableActions(
        IReadOnlyList<WorkflowTypeResponse> workflowTypes
    )
    {
        var list = new List<ActionSelectionOption>();
        foreach (var t in workflowTypes)
        {
            foreach (var act in t.Activities)
            {
                foreach (
                    var step in act.Actions.Where(static s =>
                        string.Equals(
                            s.ActionPluginKey,
                            "workflow.forms.user-entry",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                {
                    list.Add(
                        new ActionSelectionOption(step.Id, $"{t.Name} → {act.Name} → {step.Name}")
                    );
                }
            }
        }

        return list;
    }

    public static void AddOrReplaceDraftField(
        List<WorkflowActionFormFieldSpec> draftFields,
        string newFieldKey,
        string newFieldLabel,
        string conditionSourceKey,
        string conditionExpectedValue
    )
    {
        if (string.IsNullOrWhiteSpace(newFieldKey) || string.IsNullOrWhiteSpace(newFieldLabel))
        {
            return;
        }

        var rule = string.IsNullOrWhiteSpace(conditionSourceKey)
            ? null
            : new ConditionalVisibilityRuleDto(
                VisibilityMatchMode.All,
                [
                    new FieldVisibilityConditionDto(
                        conditionSourceKey.Trim(),
                        FieldVisibilityOperator.Equals,
                        conditionExpectedValue.Trim()
                    ),
                ]
            );

        _ = draftFields.RemoveAll(f =>
            string.Equals(f.AttributeKey, newFieldKey.Trim(), StringComparison.OrdinalIgnoreCase)
        );
        draftFields.Add(
            new WorkflowActionFormFieldSpec(
                newFieldKey.Trim(),
                newFieldLabel.Trim(),
                "memo",
                Order: draftFields.Count,
                SectionKey: "details",
                IsRequired: true,
                ColumnSpan: 12,
                VisibilityRule: rule
            )
        );
    }

    public static WorkflowActionFormSpec BuildCurrentSpec(
        IReadOnlyList<WorkflowActionFormFieldSpec> draftFields,
        IReadOnlyList<FormBuilderTemplateResponse> templates
    )
    {
        Guid? tmplId = templates.Count > 0 ? templates[0].Id : null;
        return new WorkflowActionFormSpec(
            draftFields,
            [
                new WorkflowFormButtonSpec(
                    "approve",
                    "Approve & Route",
                    TargetActivityKey: "final-approval",
                    SetStateLabel: "Approved",
                    Order: 0
                ),
                new WorkflowFormButtonSpec(
                    "cancel",
                    "Decline",
                    ButtonStyle: "Outline",
                    CancelWorkflow: true,
                    Order: 1
                ),
            ],
            Sections: [new WorkflowActionFormSectionSpec("details", "Intake Details", Order: 0)],
            FormBuilderTemplateId: tmplId,
            HeaderMarkup: "Review {{ WorkflowNumber }}"
        );
    }

    public static List<WorkflowActionFormFieldSpec> CreateDefaultFields()
    {
        return
        [
            new WorkflowActionFormFieldSpec(
                "RequestKind",
                "Request Category",
                "select",
                Order: 0,
                SectionKey: "details",
                IsRequired: true,
                DefaultValue: "General",
                ColumnSpan: 6,
                SelectOptions: ["General", "Grant", "Support"]
            ),
            new WorkflowActionFormFieldSpec(
                "Summary",
                "Summary Notes",
                "text",
                Order: 1,
                SectionKey: "details",
                IsRequired: true,
                ColumnSpan: 6
            ),
        ];
    }
}
