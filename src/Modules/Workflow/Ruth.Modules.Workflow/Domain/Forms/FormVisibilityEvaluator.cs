using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain.Forms;

/// <summary>Pure deterministic conditional visibility and submission validation engine for W5-S6 interactive forms.</summary>
public static class FormVisibilityEvaluator
{
    public static IReadOnlyDictionary<string, string?> MergeEffectiveValues(
        IReadOnlyList<WorkflowActionFormFieldSpec> fields,
        IReadOnlyDictionary<string, string?>? workflowAttributes,
        IReadOnlyDictionary<string, string?>? draftOverrides
    )
    {
        return MergeEffectiveDictionary(fields, workflowAttributes, draftOverrides);
    }

    public static bool IsRuleSatisfied(
        ConditionalVisibilityRuleDto? rule,
        IReadOnlyDictionary<string, string?> effectiveValues
    )
    {
        return rule is null
            || rule.Conditions is null
            || rule.Conditions.Count == 0
            || (
                rule.MatchMode == VisibilityMatchMode.All
                    ? rule.Conditions.All(c =>
                        FormVisibilityPredicateHelper.EvaluateCondition(c, effectiveValues)
                    )
                    : rule.Conditions.Any(c =>
                        FormVisibilityPredicateHelper.EvaluateCondition(c, effectiveValues)
                    )
            );
    }

    public static FormVisibilityPreviewResponse EvaluatePreview(
        WorkflowActionFormSpec spec,
        IReadOnlyDictionary<string, string?>? sampleValues
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var values = MergeEffectiveDictionary(spec.Fields, workflowAttributes: null, sampleValues);
        var sectionVisibility = BuildPreviewSectionMap(spec.Sections, values);

        var visibleAttributes = new List<string>();
        var requiredVisible = new List<string>();
        foreach (var field in spec.Fields.OrderBy(static f => f.Order))
        {
            if (IsPreviewFieldVisible(field, sectionVisibility, values))
            {
                visibleAttributes.Add(field.AttributeKey);
                if (field.IsRequired && !field.IsReadOnly)
                {
                    requiredVisible.Add(field.AttributeKey);
                }
            }
        }

        List<string> visibleSections =
        [
            .. sectionVisibility.Where(static kv => kv.Value).Select(static kv => kv.Key),
        ];
        return new FormVisibilityPreviewResponse(
            visibleSections,
            visibleAttributes,
            requiredVisible
        );
    }

    private static Dictionary<string, bool> BuildPreviewSectionMap(
        IReadOnlyList<WorkflowActionFormSectionSpec>? sections,
        Dictionary<string, string?> values
    )
    {
        var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var sec in (sections ?? []).OrderBy(static s => s.Order))
        {
            map[sec.SectionKey] = IsRuleSatisfied(sec.VisibilityRule, values);
        }

        return map;
    }

    private static bool IsPreviewFieldVisible(
        WorkflowActionFormFieldSpec field,
        Dictionary<string, bool> sectionVisibility,
        Dictionary<string, string?> values
    )
    {
        var secVisible =
            string.IsNullOrWhiteSpace(field.SectionKey)
            || !sectionVisibility.TryGetValue(field.SectionKey, out var sv)
            || sv;
        return field.IsVisible && secVisible && IsRuleSatisfied(field.VisibilityRule, values);
    }

    public static Result<FormSubmissionEvaluationResult> ValidateSubmission(
        WorkflowActionForm form,
        string selectedButtonKey,
        IReadOnlyDictionary<string, string?> workflowAttributes,
        IReadOnlyDictionary<string, string?> submittedValues
    )
    {
        ArgumentNullException.ThrowIfNull(form);
        var button = form.Buttons.FirstOrDefault(b =>
            string.Equals(
                b.ButtonKey,
                selectedButtonKey?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
        );
        if (button is null)
        {
            return Result.Failure<FormSubmissionEvaluationResult>(
                WorkflowErrors.FormSubmissionInvalid
            );
        }

        var writableDraft = FilterWritableSubmissions(form, submittedValues);
        List<WorkflowActionFormFieldSpec> specFields =
        [
            .. form.Fields.Select(static f => new WorkflowActionFormFieldSpec(
                f.AttributeKey,
                f.Label,
                f.FieldTypeKey,
                f.Order,
                f.SectionKey,
                f.IsVisible,
                f.IsReadOnly,
                f.IsRequired,
                f.DefaultValue,
                f.HelpText,
                f.ColumnSpan,
                f.SelectOptions,
                f.VisibilityRule
            )),
        ];
        var effective = MergeEffectiveDictionary(specFields, workflowAttributes, writableDraft);

        if (!button.CancelWorkflow)
        {
            var validationError = ValidateVisibleFields(form, effective);
            if (validationError is not null)
            {
                return Result.Failure<FormSubmissionEvaluationResult>(validationError);
            }
        }

        var sanitized = ExtractVisibleWritableUpdates(form, effective, writableDraft);
        return Result.Success(new FormSubmissionEvaluationResult(button, sanitized));
    }

    private static Dictionary<string, string?> MergeEffectiveDictionary(
        IReadOnlyList<WorkflowActionFormFieldSpec> fields,
        IReadOnlyDictionary<string, string?>? workflowAttributes,
        IReadOnlyDictionary<string, string?>? draftOverrides
    )
    {
        var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (field.DefaultValue is not null)
            {
                merged[field.AttributeKey] = field.DefaultValue;
            }
        }

        FormVisibilityPredicateHelper.CopyInto(merged, workflowAttributes);
        FormVisibilityPredicateHelper.CopyInto(merged, draftOverrides);
        return merged;
    }

    private static Dictionary<string, string?> FilterWritableSubmissions(
        WorkflowActionForm form,
        IReadOnlyDictionary<string, string?> submitted
    )
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var writableKeys = form
            .Fields.Where(static f => !f.IsReadOnly)
            .Select(static f => f.AttributeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, val) in submitted)
        {
            if (writableKeys.Contains(key))
            {
                result[key] = val?.Trim();
            }
        }

        return result;
    }

    private static DomainError? ValidateVisibleFields(
        WorkflowActionForm form,
        Dictionary<string, string?> effective
    )
    {
        var sectionVisibility = form.Sections.ToDictionary(
            static s => s.SectionKey,
            s => IsRuleSatisfied(s.VisibilityRule, effective),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var field in form.Fields)
        {
            if (
                field.IsReadOnly
                || !FormVisibilityPredicateHelper.IsFieldCurrentlyVisible(
                    field,
                    sectionVisibility,
                    effective
                )
            )
            {
                continue;
            }

            _ = effective.TryGetValue(field.AttributeKey, out var value);
            if (field.IsRequired && string.IsNullOrWhiteSpace(value))
            {
                return DomainError.Validation(
                    "Workflow.FormRequiredFieldMissing",
                    $"Field '{field.Label}' is required."
                );
            }

            if (
                !string.IsNullOrWhiteSpace(value)
                && !FormVisibilityPredicateHelper.IsTypeValid(field, value)
            )
            {
                return DomainError.Validation(
                    "Workflow.FormFieldTypeInvalid",
                    $"Field '{field.Label}' contains an invalid '{field.FieldTypeKey}' value."
                );
            }
        }

        return null;
    }

    private static Dictionary<string, string?> ExtractVisibleWritableUpdates(
        WorkflowActionForm form,
        Dictionary<string, string?> effective,
        Dictionary<string, string?> writableDraft
    )
    {
        var sectionVisibility = form.Sections.ToDictionary(
            static s => s.SectionKey,
            s => IsRuleSatisfied(s.VisibilityRule, effective),
            StringComparer.OrdinalIgnoreCase
        );

        var updates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in form.Fields)
        {
            if (
                field.IsReadOnly
                || !FormVisibilityPredicateHelper.IsFieldCurrentlyVisible(
                    field,
                    sectionVisibility,
                    effective
                )
            )
            {
                continue;
            }

            if (writableDraft.TryGetValue(field.AttributeKey, out var explicitVal))
            {
                updates[field.AttributeKey] = explicitVal;
            }
            else if (effective.TryGetValue(field.AttributeKey, out var defaultVal))
            {
                updates[field.AttributeKey] = defaultVal;
            }
        }

        return updates;
    }
}
