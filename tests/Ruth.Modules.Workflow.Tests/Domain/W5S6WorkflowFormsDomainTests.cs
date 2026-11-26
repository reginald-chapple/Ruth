using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Forms;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Domain;

public sealed class W5S6WorkflowFormsDomainTests
{
    [Fact]
    public void FormBuilderTemplate_AndWorkflowActionForm_ValidateStructureAndDuplicates()
    {
        var invalidTmpl = FormBuilderTemplate.Create(new FormBuilderTemplateSpec("", "bad slug!"));
        Assert.True(invalidTmpl.IsFailure);

        var validTmpl = FormBuilderTemplate.Create(
            new FormBuilderTemplateSpec(
                "Community Grant Form",
                "community-grant-form",
                ConfirmationTitle: "Grant Submitted",
                AllowNotes: true
            )
        );
        Assert.True(validTmpl.IsSuccess);
        Assert.True(
            validTmpl
                .Value.Update(
                    new FormBuilderTemplateSpec(
                        "Updated Grant Form",
                        "community-grant-form",
                        ConfirmationTitle: "Received"
                    )
                )
                .IsSuccess
        );

        var actionTypeId = WorkflowActionTypeId.Create();
        var missingSectionRef = WorkflowActionForm.Create(
            actionTypeId,
            new WorkflowActionFormSpec(
                [
                    new WorkflowActionFormFieldSpec(
                        "Amount",
                        "Amount",
                        "currency",
                        SectionKey: "unknown-section"
                    ),
                ],
                [new WorkflowFormButtonSpec("submit", "Submit")]
            )
        );
        Assert.True(missingSectionRef.IsFailure);

        var validForm = WorkflowActionForm.Create(actionTypeId, BuildConditionalGrantFormSpec());
        Assert.True(validForm.IsSuccess);
        Assert.Equal(2, validForm.Value.Sections.Count);
        Assert.Equal(5, validForm.Value.Fields.Count);
        Assert.Equal(2, validForm.Value.Buttons.Count);
    }

    [Theory]
    [InlineData(FieldVisibilityOperator.Equals, "Grant", "grant", true)]
    [InlineData(FieldVisibilityOperator.NotEquals, "Support", "grant", true)]
    [InlineData(FieldVisibilityOperator.Contains, "Micro-Grant Program", "grant", true)]
    [InlineData(FieldVisibilityOperator.IsEmpty, "   ", null, true)]
    [InlineData(FieldVisibilityOperator.IsNotEmpty, "filled", null, true)]
    [InlineData(FieldVisibilityOperator.GreaterThan, "1500", "1000", true)]
    [InlineData(FieldVisibilityOperator.LessThan, "500", "1000", true)]
    [InlineData(FieldVisibilityOperator.IsTrue, "yes", null, true)]
    [InlineData(FieldVisibilityOperator.IsFalse, "no", null, true)]
    public void FormVisibilityEvaluator_IsRuleSatisfied_SupportsAllOperatorsAndMatchModes(
        FieldVisibilityOperator op,
        string? sampleVal,
        string? expectedVal,
        bool expectedVisible
    )
    {
        var values = Dict(("TriggerAttr", sampleVal), ("Secondary", "ok"));

        var allRule = new ConditionalVisibilityRuleDto(
            VisibilityMatchMode.All,
            [
                new FieldVisibilityConditionDto("TriggerAttr", op, expectedVal),
                new FieldVisibilityConditionDto("Secondary", FieldVisibilityOperator.Equals, "ok"),
            ]
        );
        Assert.Equal(expectedVisible, FormVisibilityEvaluator.IsRuleSatisfied(allRule, values));

        var anyRule = new ConditionalVisibilityRuleDto(
            VisibilityMatchMode.Any,
            [
                new FieldVisibilityConditionDto(
                    "Secondary",
                    FieldVisibilityOperator.Equals,
                    "never"
                ),
                new FieldVisibilityConditionDto("TriggerAttr", op, expectedVal),
            ]
        );
        Assert.Equal(expectedVisible, FormVisibilityEvaluator.IsRuleSatisfied(anyRule, values));
    }

    [Fact]
    public void FormVisibilityEvaluator_ValidateSubmission_EnforcesVisibleRequiredTypesReadOnlyAndCancel()
    {
        var spec = BuildConditionalGrantFormSpec();
        var form = WorkflowActionForm.Create(WorkflowActionTypeId.Create(), spec).Value;
        var baseAttrs = Dict(("InternalAuditCode", "LOCKED-001"));

        var previewGeneral = FormVisibilityEvaluator.EvaluatePreview(
            spec,
            Dict(("RequestKind", "General"))
        );
        Assert.DoesNotContain(
            "grant-section",
            previewGeneral.VisibleSectionKeys,
            StringComparer.OrdinalIgnoreCase
        );
        Assert.DoesNotContain(
            "GrantAmount",
            previewGeneral.RequiredVisibleAttributeKeys,
            StringComparer.OrdinalIgnoreCase
        );

        var generalSubmit = FormVisibilityEvaluator.ValidateSubmission(
            form,
            "approve",
            baseAttrs,
            Dict(
                ("RequestKind", "General"),
                ("ApplicantEmail", "member@example.org"),
                ("InternalAuditCode", "HACKED")
            )
        );
        Assert.True(generalSubmit.IsSuccess);
        Assert.False(generalSubmit.Value.SanitizedAttributes.ContainsKey("InternalAuditCode"));
        Assert.False(generalSubmit.Value.SanitizedAttributes.ContainsKey("GrantAmount"));

        var missingGrantAmount = FormVisibilityEvaluator.ValidateSubmission(
            form,
            "approve",
            baseAttrs,
            Dict(("RequestKind", "Grant"), ("ApplicantEmail", "member@example.org"))
        );
        Assert.True(missingGrantAmount.IsFailure);
        Assert.Equal("Workflow.FormRequiredFieldMissing", missingGrantAmount.PrimaryError.Code);

        var badCurrency = FormVisibilityEvaluator.ValidateSubmission(
            form,
            "approve",
            baseAttrs,
            Dict(
                ("RequestKind", "Grant"),
                ("ApplicantEmail", "member@example.org"),
                ("GrantAmount", "not-a-number"),
                ("NeedByDate", "2026-12-01")
            )
        );
        Assert.True(badCurrency.IsFailure);
        Assert.Equal("Workflow.FormFieldTypeInvalid", badCurrency.PrimaryError.Code);

        var validGrant = FormVisibilityEvaluator.ValidateSubmission(
            form,
            "approve",
            baseAttrs,
            Dict(
                ("RequestKind", "Grant"),
                ("ApplicantEmail", "member@example.org"),
                ("GrantAmount", "750.00"),
                ("NeedByDate", "2026-12-01")
            )
        );
        Assert.True(validGrant.IsSuccess);
        Assert.Equal("750.00", validGrant.Value.SanitizedAttributes["GrantAmount"]);

        var cancelBypassesRequired = FormVisibilityEvaluator.ValidateSubmission(
            form,
            "decline",
            baseAttrs,
            Dict(("RequestKind", "Grant"))
        );
        Assert.True(cancelBypassesRequired.IsSuccess);
        Assert.True(cancelBypassesRequired.Value.SelectedButton.CancelWorkflow);
    }

    private static Dictionary<string, string?> Dict(params (string Key, string? Val)[] pairs)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in pairs)
        {
            map[k] = v;
        }

        return map;
    }

    private static WorkflowActionFormSpec BuildConditionalGrantFormSpec()
    {
        var grantRule = new ConditionalVisibilityRuleDto(
            VisibilityMatchMode.All,
            [
                new FieldVisibilityConditionDto(
                    "RequestKind",
                    FieldVisibilityOperator.Equals,
                    "Grant"
                ),
            ]
        );

        return new WorkflowActionFormSpec(
            Fields:
            [
                new WorkflowActionFormFieldSpec(
                    "RequestKind",
                    "Request Kind",
                    "select",
                    Order: 0,
                    SectionKey: "core-section",
                    IsRequired: true,
                    DefaultValue: "General",
                    SelectOptions: ["General", "Grant"]
                ),
                new WorkflowActionFormFieldSpec(
                    "ApplicantEmail",
                    "Applicant Email",
                    "email",
                    Order: 1,
                    SectionKey: "core-section",
                    IsRequired: true
                ),
                new WorkflowActionFormFieldSpec(
                    "InternalAuditCode",
                    "Audit Code",
                    "text",
                    Order: 2,
                    SectionKey: "core-section",
                    IsReadOnly: true
                ),
                new WorkflowActionFormFieldSpec(
                    "GrantAmount",
                    "Requested Grant Amount",
                    "currency",
                    Order: 3,
                    SectionKey: "grant-section",
                    IsRequired: true
                ),
                new WorkflowActionFormFieldSpec(
                    "NeedByDate",
                    "Need By Date",
                    "date",
                    Order: 4,
                    SectionKey: "grant-section",
                    IsRequired: true
                ),
            ],
            Buttons:
            [
                new WorkflowFormButtonSpec(
                    "approve",
                    "Approve",
                    TargetActivityKey: "approved",
                    Order: 0
                ),
                new WorkflowFormButtonSpec("decline", "Decline", CancelWorkflow: true, Order: 1),
            ],
            Sections:
            [
                new WorkflowActionFormSectionSpec("core-section", "Applicant Info", Order: 0),
                new WorkflowActionFormSectionSpec(
                    "grant-section",
                    "Grant Justification",
                    Order: 1,
                    VisibilityRule: grantRule
                ),
            ]
        );
    }
}
