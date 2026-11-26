using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain.Forms;

/// <summary>Interactive field definition inside a WorkflowActionForm.</summary>
public sealed class WorkflowActionFormField : Entity<WorkflowActionFormFieldId>
{
    private List<string> _selectOptions = [];

    private WorkflowActionFormField() { }

    private WorkflowActionFormField(WorkflowActionFormFieldId id)
        : base(id) { }

    public WorkflowActionFormId WorkflowActionFormId { get; private set; }
    public string? SectionKey { get; private set; }
    public string AttributeKey { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string FieldTypeKey { get; private set; } = "text";
    public int Order { get; private set; }
    public bool IsVisible { get; private set; } = true;
    public bool IsReadOnly { get; private set; }
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }
    public string? HelpText { get; private set; }
    public int ColumnSpan { get; private set; } = 12;
    public ConditionalVisibilityRuleDto? VisibilityRule { get; private set; }

    public IReadOnlyList<string> SelectOptions
    {
        get => _selectOptions;
        private set => _selectOptions = [.. value];
    }

    internal static Result<WorkflowActionFormField> Create(
        WorkflowActionFormId formId,
        WorkflowActionFormFieldSpec spec
    )
    {
        return
            !HasValidCoreBounds(spec)
            || !HasValidVisibilityRule(spec.VisibilityRule)
            || !TryNormalizeSection(spec.SectionKey, out var normalizedSection)
            ? Result.Failure<WorkflowActionFormField>(WorkflowErrors.ActionFormInvalid)
            : Result.Success(BuildEntity(formId, spec, normalizedSection));
    }

    private static bool HasValidCoreBounds(WorkflowActionFormFieldSpec spec)
    {
        return !string.IsNullOrWhiteSpace(spec.AttributeKey)
            && !string.IsNullOrWhiteSpace(spec.Label)
            && spec.Order >= 0
            && spec.ColumnSpan is >= 1 and <= 12;
    }

    private static bool HasValidVisibilityRule(ConditionalVisibilityRuleDto? rule)
    {
        return rule is null || (rule.Conditions is not null && rule.Conditions.Count > 0);
    }

    private static bool TryNormalizeSection(string? rawSection, out string? normalized)
    {
        if (string.IsNullOrWhiteSpace(rawSection))
        {
            normalized = null;
            return true;
        }

        var ok = WorkflowTextNormalizer.TryNormalizeSlug(rawSection, out var slug);
        normalized = ok ? slug : null;
        return ok;
    }

    private static WorkflowActionFormField BuildEntity(
        WorkflowActionFormId formId,
        WorkflowActionFormFieldSpec spec,
        string? normalizedSection
    )
    {
        List<string> options =
        [
            .. (spec.SelectOptions ?? [])
                .Where(static o => !string.IsNullOrWhiteSpace(o))
                .Select(static o => o.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];

        return new WorkflowActionFormField(WorkflowActionFormFieldId.Create())
        {
            WorkflowActionFormId = formId,
            SectionKey = normalizedSection,
            AttributeKey = spec.AttributeKey.Trim(),
            Label = spec.Label.Trim(),
            FieldTypeKey = string.IsNullOrWhiteSpace(spec.FieldTypeKey)
                ? "text"
                : spec.FieldTypeKey.Trim().ToLowerInvariant(),
            Order = spec.Order,
            IsVisible = spec.IsVisible,
            IsReadOnly = spec.IsReadOnly,
            IsRequired = spec.IsRequired,
            DefaultValue = spec.DefaultValue,
            HelpText = WorkflowTextNormalizer.TrimOrNull(spec.HelpText, 500),
            ColumnSpan = spec.ColumnSpan,
            _selectOptions = options,
            VisibilityRule = spec.VisibilityRule,
        };
    }
}
