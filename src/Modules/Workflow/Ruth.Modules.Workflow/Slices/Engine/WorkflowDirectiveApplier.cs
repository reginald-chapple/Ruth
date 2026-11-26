using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Infrastructure;

namespace Ruth.Modules.Workflow.Slices.Engine;

internal static class WorkflowDirectiveApplier
{
    public static bool Apply(
        WorkflowDbContext dbContext,
        WorkflowInstance instance,
        WorkflowType definition,
        WorkflowActivity activity,
        WorkflowAction action,
        WorkflowActionType? actionDef,
        WorkflowActionDirective directive,
        DateTimeOffset now,
        out bool paused
    )
    {
        ApplyStateAndAttributeMutations(instance, definition, activity, directive, now);
        var routed = ApplyOutcomeSwitch(
            dbContext,
            instance,
            definition,
            activity,
            action,
            directive,
            now,
            out paused
        );
        return routed
            || ApplyDefinitionCompletionFlags(
                dbContext,
                instance,
                definition,
                activity,
                actionDef,
                now
            );
    }

    public static void FinalizeCompletedWorkflow(
        WorkflowDbContext dbContext,
        WorkflowInstance instance,
        WorkflowType definition,
        DateTimeOffset now,
        string? summary = null
    )
    {
        var completeRes = instance.Complete(
            summary ?? $"Workflow '{instance.WorkflowNumber}' completed all activities.",
            definition.LoggingLevel,
            now
        );
        if (completeRes.IsSuccess)
        {
            dbContext.EnqueueIntegrationEvent(
                new WorkflowCompletedIntegrationEvent(
                    Guid.CreateVersion7(now),
                    now,
                    instance.Id.Value,
                    instance.WorkflowTypeId.Value,
                    instance.WorkflowNumber,
                    instance.Name,
                    instance.StateLabel,
                    instance.InitiatorPersonId,
                    instance.PrimaryEntityTypeKey,
                    instance.PrimaryEntityId
                )
            );
        }
    }

    private static void ApplyStateAndAttributeMutations(
        WorkflowInstance instance,
        WorkflowType definition,
        WorkflowActivity activity,
        WorkflowActionDirective directive,
        DateTimeOffset now
    )
    {
        ApplyAttributeUpdates(instance, activity, directive);
        instance.UpdateStateLabel(directive.NewStateLabel);
        ApplyAssignmentIfRequested(activity, directive);
        instance.AddLog(definition.LoggingLevel, WorkflowLogKind.Action, directive.Summary, now);
    }

    private static void ApplyAttributeUpdates(
        WorkflowInstance instance,
        WorkflowActivity activity,
        WorkflowActionDirective directive
    )
    {
        if (directive.UpdatedWorkflowAttributes is not null)
        {
            instance.MergeAttributes(directive.UpdatedWorkflowAttributes);
        }

        if (directive.UpdatedActivityAttributes is not null)
        {
            activity.MergeAttributes(directive.UpdatedActivityAttributes);
        }
    }

    private static void ApplyAssignmentIfRequested(
        WorkflowActivity activity,
        WorkflowActionDirective directive
    )
    {
        if (directive.AssignPersonId.HasValue || directive.AssignGroupId.HasValue)
        {
            activity.Assign(
                directive.AssignPersonId ?? activity.AssignedPersonId,
                directive.AssignGroupId ?? activity.AssignedGroupId
            );
        }
    }

    private static bool ApplyOutcomeSwitch(
        WorkflowDbContext dbContext,
        WorkflowInstance instance,
        WorkflowType definition,
        WorkflowActivity activity,
        WorkflowAction action,
        WorkflowActionDirective directive,
        DateTimeOffset now,
        out bool paused
    )
    {
        paused = false;
        switch (directive.OutcomeKind)
        {
            case WorkflowActionOutcomeKind.WaitForInput:
                paused = true;
                return true;

            case WorkflowActionOutcomeKind.DelayUntil:
                action.MarkCompleted(now);
                paused = true;
                instance.SuspendUntil(directive.ResumeAtUtc ?? now.AddMinutes(1), now);
                return true;

            case WorkflowActionOutcomeKind.CancelWorkflow:
                action.MarkCompleted(now);
                _ = instance.Cancel(directive.Summary, definition.LoggingLevel, now);
                return true;

            case WorkflowActionOutcomeKind.CompleteWorkflow:
                action.MarkCompleted(now);
                FinalizeCompletedWorkflow(dbContext, instance, definition, now, directive.Summary);
                return true;

            case WorkflowActionOutcomeKind.ActivateActivity:
                action.MarkCompleted(now);
                ActivateTargetIfPresent(instance, definition, directive.TargetActivityKey, now);
                return false;

            case WorkflowActionOutcomeKind.CompleteActivity:
                action.MarkCompleted(now);
                activity.MarkCompleted(now);
                return true;
            case WorkflowActionOutcomeKind.Continue:
            default:
                action.MarkCompleted(now);
                return false;
        }
    }

    private static void ActivateTargetIfPresent(
        WorkflowInstance instance,
        WorkflowType definition,
        string? targetKey,
        DateTimeOffset now
    )
    {
        if (string.IsNullOrWhiteSpace(targetKey))
        {
            return;
        }

        var target = definition.FindActivityByKey(targetKey);
        if (target is not null)
        {
            _ = instance.ActivateActivity(target, definition.LoggingLevel, now);
        }
    }

    private static bool ApplyDefinitionCompletionFlags(
        WorkflowDbContext dbContext,
        WorkflowInstance instance,
        WorkflowType definition,
        WorkflowActivity activity,
        WorkflowActionType? actionDef,
        DateTimeOffset now
    )
    {
        if (actionDef?.IsWorkflowCompletedOnSuccess == true)
        {
            FinalizeCompletedWorkflow(dbContext, instance, definition, now);
            return true;
        }

        if (actionDef?.IsActivityCompletedOnSuccess == true)
        {
            activity.MarkCompleted(now);
            return true;
        }

        return false;
    }
}
