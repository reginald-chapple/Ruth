using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain.Forms;

/// <summary>Visual grouping section inside a WorkflowActionForm.</summary>
public sealed class WorkflowActionFormSection : Entity<WorkflowActionFormSectionId>
{
    private WorkflowActionFormSection() { }

    private WorkflowActionFormSection(WorkflowActionFormSectionId id)
        : base(id) { }

    public WorkflowActionFormId WorkflowActionFormId { get; private set; }
    public string SectionKey { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }
    public bool ShowHeadingSeparator { get; private set; } = true;
    public ConditionalVisibilityRuleDto? VisibilityRule { get; private set; }

    internal static Result<WorkflowActionFormSection> Create(
        WorkflowActionFormId formId,
        WorkflowActionFormSectionSpec spec
    )
    {
        return !WorkflowTextNormalizer.TryNormalizeSlug(spec.SectionKey, out var key)
            || string.IsNullOrWhiteSpace(spec.Title)
            || spec.Order < 0
                ? Result.Failure<WorkflowActionFormSection>(WorkflowErrors.ActionFormInvalid)
            : spec.VisibilityRule is not null
            && (spec.VisibilityRule.Conditions is null || spec.VisibilityRule.Conditions.Count == 0)
                ? Result.Failure<WorkflowActionFormSection>(WorkflowErrors.ActionFormInvalid)
            : Result.Success(
                new WorkflowActionFormSection(WorkflowActionFormSectionId.Create())
                {
                    WorkflowActionFormId = formId,
                    SectionKey = key,
                    Title = spec.Title.Trim(),
                    Description = WorkflowTextNormalizer.TrimOrNull(spec.Description, 1000),
                    Order = spec.Order,
                    ShowHeadingSeparator = spec.ShowHeadingSeparator,
                    VisibilityRule = spec.VisibilityRule,
                }
            );
    }
}
