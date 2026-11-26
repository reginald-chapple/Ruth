using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices.Actions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class GetWorkflowEntryFormQueryHandler(
    WorkflowDbContext dbContext,
    IMergeTemplateEngine mergeEngine
) : IQueryHandler<GetWorkflowEntryFormQuery, WorkflowEntryFormViewResponse>
{
    public async Task<Result<WorkflowEntryFormViewResponse>> HandleAsync(
        GetWorkflowEntryFormQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var wfId = WorkflowId.From(query.WorkflowId);
        var instance = await dbContext
            .Workflows.AsNoTracking()
            .AsSplitQuery()
            .Include(static w => w.Activities)
                .ThenInclude(static a => a.Actions)
            .FirstOrDefaultAsync(w => w.Id == wfId, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<WorkflowEntryFormViewResponse>(WorkflowErrors.WorkflowNotFound);
        }

        var (activity, waitingAction) = LocateWaitingFormAction(instance, query.ActivityId);
        if (activity is null || waitingAction is null)
        {
            return Result.Failure<WorkflowEntryFormViewResponse>(WorkflowErrors.ActionFormNotFound);
        }

        var form = await dbContext
            .WorkflowActionForms.AsNoTracking()
            .AsSplitQuery()
            .Include(static f => f.Sections)
            .Include(static f => f.Fields)
            .FirstOrDefaultAsync(
                f => f.WorkflowActionTypeId == waitingAction.WorkflowActionTypeId,
                cancellationToken
            );
        if (form is null)
        {
            return Result.Failure<WorkflowEntryFormViewResponse>(WorkflowErrors.ActionFormNotFound);
        }

        var template = form.FormBuilderTemplateId.HasValue
            ? await dbContext
                .FormBuilderTemplates.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == form.FormBuilderTemplateId.Value,
                    cancellationToken
                )
            : null;

        return Result.Success(
            await BuildEntryViewAsync(
                instance,
                activity,
                waitingAction,
                form,
                template,
                query.DraftValues,
                cancellationToken
            )
        );
    }

    private async Task<WorkflowEntryFormViewResponse> BuildEntryViewAsync(
        WorkflowInstance instance,
        WorkflowActivity activity,
        WorkflowAction action,
        WorkflowActionForm form,
        FormBuilderTemplate? template,
        IReadOnlyDictionary<string, string?>? draftValues,
        CancellationToken ct
    )
    {
        var effective = ComputeEffectiveValues(form, instance.Attributes, draftValues);
        var sectionVisibility = form.Sections.ToDictionary(
            static s => s.SectionKey,
            s => FormVisibilityEvaluator.IsRuleSatisfied(s.VisibilityRule, effective),
            StringComparer.OrdinalIgnoreCase
        );
        var fieldViews = BuildFieldViews(form, sectionVisibility, effective);
        var sections = BuildSectionViews(form, sectionVisibility, fieldViews);
        List<WorkflowEntryFieldViewDto> ungrouped =
        [
            .. fieldViews.Where(static fv => string.IsNullOrWhiteSpace(fv.SectionKey)),
        ];

        return new WorkflowEntryFormViewResponse(
            instance.Id.Value,
            instance.WorkflowNumber,
            instance.Name,
            instance.StateLabel,
            activity.Id.Value,
            activity.Name,
            action.Id.Value,
            action.WorkflowActionTypeId.Value,
            await RenderOptionalMergeAsync(
                form.HeaderMarkup ?? template?.FormHeaderHtml,
                instance,
                effective,
                ct
            ),
            await RenderOptionalMergeAsync(
                form.FooterMarkup ?? template?.FormFooterHtml,
                instance,
                effective,
                ct
            ),
            form.AllowPersonEntry || (template?.IncludePersonEntrySection == true),
            sections,
            ungrouped,
            form.Buttons
        );
    }

    private static IReadOnlyDictionary<string, string?> ComputeEffectiveValues(
        WorkflowActionForm form,
        IReadOnlyDictionary<string, string?> workflowAttributes,
        IReadOnlyDictionary<string, string?>? draftValues
    )
    {
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
        return FormVisibilityEvaluator.MergeEffectiveValues(
            specFields,
            workflowAttributes,
            draftValues
        );
    }

    private static List<WorkflowEntrySectionViewDto> BuildSectionViews(
        WorkflowActionForm form,
        Dictionary<string, bool> sectionVisibility,
        List<WorkflowEntryFieldViewDto> fieldViews
    )
    {
        return
        [
            .. form
                .Sections.OrderBy(static s => s.Order)
                .Select(s => new WorkflowEntrySectionViewDto(
                    s.SectionKey,
                    s.Title,
                    s.Description,
                    s.ShowHeadingSeparator,
                    sectionVisibility.GetValueOrDefault(s.SectionKey, defaultValue: true),
                    [
                        .. fieldViews.Where(fv =>
                            string.Equals(
                                fv.SectionKey,
                                s.SectionKey,
                                StringComparison.OrdinalIgnoreCase
                            )
                        ),
                    ]
                )),
        ];
    }

    private static List<WorkflowEntryFieldViewDto> BuildFieldViews(
        WorkflowActionForm form,
        Dictionary<string, bool> sectionVisibility,
        IReadOnlyDictionary<string, string?> effective
    )
    {
        return
        [
            .. form
                .Fields.OrderBy(static f => f.Order)
                .Select(f =>
                {
                    var secVisible =
                        string.IsNullOrWhiteSpace(f.SectionKey)
                        || !sectionVisibility.TryGetValue(f.SectionKey, out var sv)
                        || sv;
                    var visible =
                        f.IsVisible
                        && secVisible
                        && FormVisibilityEvaluator.IsRuleSatisfied(f.VisibilityRule, effective);
                    _ = effective.TryGetValue(f.AttributeKey, out var currentVal);

                    return new WorkflowEntryFieldViewDto(
                        f.Id.Value,
                        f.SectionKey,
                        f.AttributeKey,
                        f.Label,
                        f.FieldTypeKey,
                        currentVal,
                        visible,
                        f.IsReadOnly,
                        f.IsRequired,
                        f.HelpText,
                        f.ColumnSpan,
                        f.SelectOptions,
                        f.VisibilityRule
                    );
                }),
        ];
    }

    private static (WorkflowActivity? Activity, WorkflowAction? Action) LocateWaitingFormAction(
        WorkflowInstance instance,
        Guid? requestedActivityId
    )
    {
        foreach (
            var act in instance
                .Activities.Where(a =>
                    !a.IsCompleted
                    && (!requestedActivityId.HasValue || a.Id.Value == requestedActivityId.Value)
                )
                .OrderBy(static a => a.Order)
        )
        {
            var formAct = act
                .Actions.OrderBy(static x => x.Order)
                .FirstOrDefault(static x =>
                    !x.IsCompleted
                    && string.Equals(
                        x.ActionPluginKey,
                        UserEntryFormWorkflowAction.Key,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            if (formAct is not null)
            {
                return (act, formAct);
            }
        }

        return (null, null);
    }

    private async Task<string?> RenderOptionalMergeAsync(
        string? markup,
        WorkflowInstance instance,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(markup))
        {
            return null;
        }

        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["WorkflowNumber"] = instance.WorkflowNumber,
            ["WorkflowName"] = instance.Name,
            ["StateLabel"] = instance.StateLabel,
        };
        foreach (var (k, v) in values)
        {
            ctx[k] = v;
        }

        var res = await mergeEngine.RenderAsync(markup, ctx, options: null, ct);
        return res.IsSuccess ? res.Value.RenderedContent : markup;
    }
}
