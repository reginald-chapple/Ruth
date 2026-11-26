using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices.Engine;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class SubmitWorkflowEntryFormCommandHandler(
    WorkflowDbContext dbContext,
    WorkflowExecutionEngine engine,
    IMergeTemplateEngine mergeEngine,
    IClock clock
) : ICommandHandler<SubmitWorkflowEntryFormCommand, WorkflowFormSubmissionResponse>
{
    public async Task<Result<WorkflowFormSubmissionResponse>> HandleAsync(
        SubmitWorkflowEntryFormCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var loaded = await LoadSubmissionAggregateAsync(command, cancellationToken);
        if (loaded.IsFailure)
        {
            return Result.Failure<WorkflowFormSubmissionResponse>(loaded.PrimaryError);
        }

        var (instance, wfType, activity, action, form) = loaded.Value;
        var evalRes = FormVisibilityEvaluator.ValidateSubmission(
            form,
            command.SelectedButtonKey,
            instance.Attributes,
            command.FieldValues ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        );
        if (evalRes.IsFailure)
        {
            return Result.Failure<WorkflowFormSubmissionResponse>(evalRes.PrimaryError);
        }

        ApplySubmittedValuesAndRouting(
            instance,
            wfType,
            activity,
            action,
            evalRes.Value,
            command,
            clock.UtcNow
        );

        if (!instance.IsTerminal)
        {
            _ = await engine.ProcessInstanceAsync(instance, wfType, cancellationToken);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildSubmissionResponseAsync(
            instance,
            wfType.Name,
            form,
            evalRes.Value.SelectedButton.ButtonKey,
            cancellationToken
        );
    }

    private async Task<Result<LoadedFormSubmission>> LoadSubmissionAggregateAsync(
        SubmitWorkflowEntryFormCommand command,
        CancellationToken ct
    )
    {
        var wfId = WorkflowId.From(command.WorkflowId);
        var instance = await dbContext
            .Workflows.AsSplitQuery()
            .Include(static w => w.Activities)
                .ThenInclude(static a => a.Actions)
            .Include(static w => w.Logs)
            .FirstOrDefaultAsync(w => w.Id == wfId, ct);
        if (instance is null)
        {
            return Result.Failure<LoadedFormSubmission>(WorkflowErrors.WorkflowNotFound);
        }

        if (instance.IsTerminal)
        {
            return Result.Failure<LoadedFormSubmission>(WorkflowErrors.WorkflowAlreadyClosed);
        }

        var actionId = WorkflowActionId.From(command.ActionId);
        var activity = instance.Activities.FirstOrDefault(a =>
            !a.IsCompleted && a.Actions.Any(act => act.Id == actionId && !act.IsCompleted)
        );
        var action = activity?.Actions.FirstOrDefault(act => act.Id == actionId);
        if (activity is null || action is null)
        {
            return Result.Failure<LoadedFormSubmission>(WorkflowErrors.ActionFormNotFound);
        }

        var form = await dbContext
            .WorkflowActionForms.AsSplitQuery()
            .Include(static f => f.Sections)
            .Include(static f => f.Fields)
            .FirstOrDefaultAsync(f => f.WorkflowActionTypeId == action.WorkflowActionTypeId, ct);
        if (form is null)
        {
            return Result.Failure<LoadedFormSubmission>(WorkflowErrors.ActionFormNotFound);
        }

        var wfType = await dbContext
            .WorkflowTypes.AsNoTracking()
            .AsSplitQuery()
            .Include(static t => t.Activities)
                .ThenInclude(static a => a.Actions)
            .FirstOrDefaultAsync(t => t.Id == instance.WorkflowTypeId, ct);

        return wfType is null
            ? Result.Failure<LoadedFormSubmission>(WorkflowErrors.WorkflowTypeNotFound)
            : Result.Success(new LoadedFormSubmission(instance, wfType, activity, action, form));
    }

    private static void ApplySubmittedValuesAndRouting(
        WorkflowInstance instance,
        WorkflowType wfType,
        WorkflowActivity activity,
        WorkflowAction action,
        FormSubmissionEvaluationResult evaluated,
        SubmitWorkflowEntryFormCommand command,
        DateTimeOffset now
    )
    {
        var mergedUpdates = MergeSubmitterMetadata(evaluated.SanitizedAttributes, command);
        instance.MergeAttributes(mergedUpdates);
        var btn = evaluated.SelectedButton;
        instance.UpdateStateLabel(btn.SetStateLabel);
        action.MarkCompleted(now, btn.ButtonKey);

        instance.AddLog(
            wfType.LoggingLevel,
            WorkflowLogKind.FormSubmission,
            $"Submitted form '{activity.Name}' via '{btn.Label}'.",
            now,
            command.ActorPersonId
        );

        if (btn.CancelWorkflow)
        {
            _ = instance.Cancel(
                $"Cancelled via form action '{btn.Label}'.",
                wfType.LoggingLevel,
                now,
                command.ActorPersonId
            );
            return;
        }

        if (btn.CompleteCurrentActivity)
        {
            activity.MarkCompleted(now);
        }

        if (!string.IsNullOrWhiteSpace(btn.TargetActivityKey))
        {
            var target = wfType.FindActivityByKey(btn.TargetActivityKey);
            if (target is not null)
            {
                _ = instance.ActivateActivity(target, wfType.LoggingLevel, now);
            }
        }
    }

    private static Dictionary<string, string?> MergeSubmitterMetadata(
        IReadOnlyDictionary<string, string?> sanitized,
        SubmitWorkflowEntryFormCommand command
    )
    {
        var dict = new Dictionary<string, string?>(sanitized, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(command.SubmitterFirstName))
        {
            dict["FirstName"] = command.SubmitterFirstName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(command.SubmitterLastName))
        {
            dict["LastName"] = command.SubmitterLastName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(command.SubmitterEmail))
        {
            dict["Email"] = command.SubmitterEmail.Trim();
        }

        if (!string.IsNullOrWhiteSpace(command.Note))
        {
            dict["SubmissionNote"] = command.Note.Trim();
        }

        return dict;
    }

    private async Task<Result<WorkflowFormSubmissionResponse>> BuildSubmissionResponseAsync(
        WorkflowInstance instance,
        string workflowTypeName,
        WorkflowActionForm form,
        string buttonKey,
        CancellationToken ct
    )
    {
        var template = form.FormBuilderTemplateId.HasValue
            ? await dbContext
                .FormBuilderTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == form.FormBuilderTemplateId.Value, ct)
            : null;

        var title = template?.ConfirmationTitle ?? "Form Submitted";
        var msgMarkup = template?.ConfirmationMessageTemplate
            ?? $"Reference {instance.WorkflowNumber} recorded.";

        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["WorkflowNumber"] = instance.WorkflowNumber,
            ["WorkflowName"] = instance.Name,
            ["StateLabel"] = instance.StateLabel,
        };
        foreach (var (k, v) in instance.Attributes)
        {
            ctx[k] = v;
        }

        var rendered = await mergeEngine.RenderAsync(msgMarkup, ctx, options: null, ct);
        var message = rendered.IsSuccess ? rendered.Value.RenderedContent : msgMarkup;

        return Result.Success(
            new WorkflowFormSubmissionResponse(
                WorkflowProjectionMapper.MapInstance(instance, workflowTypeName),
                buttonKey,
                title,
                message,
                template?.CompletionRedirectUrl
            )
        );
    }

    private sealed record LoadedFormSubmission(
        WorkflowInstance Instance,
        WorkflowType WorkflowType,
        WorkflowActivity Activity,
        WorkflowAction Action,
        WorkflowActionForm Form
    );
}
