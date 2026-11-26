using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Workflow.Slices.Engine;

internal sealed class WorkflowExecutionEngine(
    WorkflowDbContext dbContext,
    IEnumerable<IWorkflowAction> plugins,
    IClock clock
)
{
    public const int MaxStepsPerRun = 100;

    private readonly Dictionary<string, IWorkflowAction> _pluginMap = plugins.ToDictionary(
        static p => p.ActionKey,
        StringComparer.OrdinalIgnoreCase
    );

    public async Task<Result> ProcessInstanceAsync(
        WorkflowInstance instance,
        WorkflowType definition,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsReadyForProcessing(instance, clock.UtcNow))
        {
            return Result.Success();
        }

        instance.ResumeProcessing(clock.UtcNow);
        var actionLookup = definition
            .Activities.SelectMany(static a => a.Actions)
            .ToDictionary(static act => act.Id);

        return await RunLoopAsync(instance, definition, actionLookup, cancellationToken);
    }

    private async Task<Result> RunLoopAsync(
        WorkflowInstance instance,
        WorkflowType definition,
        Dictionary<WorkflowActionTypeId, WorkflowActionType> actionLookup,
        CancellationToken ct
    )
    {
        var stepCount = 0;
        while (!instance.IsTerminal)
        {
            List<WorkflowActivity> openActivities =
            [
                .. instance
                    .Activities.Where(static a => !a.IsCompleted)
                    .OrderBy(static a => a.Order),
            ];
            if (openActivities.Count == 0)
            {
                WorkflowDirectiveApplier.FinalizeCompletedWorkflow(
                    dbContext,
                    instance,
                    definition,
                    clock.UtcNow
                );
                return Result.Success();
            }

            var sweep = await SweepOpenActivitiesAsync(
                instance,
                definition,
                openActivities,
                actionLookup,
                stepCount,
                ct
            );
            if (sweep.FailedError is not null)
            {
                return Result.Failure(sweep.FailedError);
            }

            stepCount = sweep.UpdatedStepCount;
            if (instance.IsTerminal || instance.Status == WorkflowInstanceStatus.Suspended)
            {
                return Result.Success();
            }

            if (!sweep.MadeProgress)
            {
                FinishIdlePass(instance, definition, sweep.WaitingOrSuspended);
                return Result.Success();
            }
        }

        return Result.Success();
    }

    private async Task<SweepOutcome> SweepOpenActivitiesAsync(
        WorkflowInstance instance,
        WorkflowType definition,
        List<WorkflowActivity> openActivities,
        Dictionary<WorkflowActionTypeId, WorkflowActionType> actionLookup,
        int stepCount,
        CancellationToken ct
    )
    {
        var madeProgress = false;
        var waiting = false;

        foreach (var activity in openActivities)
        {
            if (stepCount >= MaxStepsPerRun)
            {
                instance.RecordExecutionFailure(
                    WorkflowErrors.StepBudgetExceeded.Description,
                    definition.MaxProcessingAttempts,
                    definition.ProcessingIntervalSeconds,
                    clock.UtcNow
                );
                return new SweepOutcome(
                    stepCount,
                    MadeProgress: false,
                    WaitingOrSuspended: false,
                    WorkflowErrors.StepBudgetExceeded
                );
            }

            var actOutcome = await ExecuteActivityPassAsync(
                instance,
                definition,
                activity,
                actionLookup,
                ct
            );
            if (actOutcome.FailedError is not null)
            {
                return new SweepOutcome(
                    stepCount + actOutcome.StepsRun,
                    madeProgress,
                    waiting,
                    actOutcome.FailedError
                );
            }

            stepCount += actOutcome.StepsRun;
            madeProgress |= actOutcome.MadeProgress;
            waiting |= actOutcome.WaitingOrSuspended;

            if (instance.IsTerminal || instance.Status == WorkflowInstanceStatus.Suspended)
            {
                break;
            }
        }

        return new SweepOutcome(stepCount, madeProgress, waiting, FailedError: null);
    }

    private async Task<ActivityPassOutcome> ExecuteActivityPassAsync(
        WorkflowInstance instance,
        WorkflowType definition,
        WorkflowActivity activity,
        Dictionary<WorkflowActionTypeId, WorkflowActionType> actionLookup,
        CancellationToken ct
    )
    {
        var steps = 0;
        var progressed = false;
        activity.MarkProcessed(clock.UtcNow);

        foreach (
            var action in activity
                .Actions.Where(static a => !a.IsCompleted)
                .OrderBy(static a => a.Order)
        )
        {
            steps++;
            var stepRes = await ExecuteSingleActionAsync(
                instance,
                definition,
                activity,
                action,
                actionLookup,
                ct
            );
            if (stepRes.FailedError is not null)
            {
                return new ActivityPassOutcome(
                    steps,
                    progressed,
                    WaitingOrSuspended: false,
                    stepRes.FailedError
                );
            }

            progressed |= !stepRes.Paused;
            if (stepRes.Paused || stepRes.HaltActivity || instance.IsTerminal)
            {
                return new ActivityPassOutcome(
                    steps,
                    progressed,
                    stepRes.Paused,
                    FailedError: null
                );
            }
        }

        if (activity.Actions.All(static a => a.IsCompleted) && !activity.IsCompleted)
        {
            activity.MarkCompleted(clock.UtcNow);
            instance.AddLog(
                definition.LoggingLevel,
                WorkflowLogKind.Activity,
                $"Completed activity '{activity.Name}'.",
                clock.UtcNow
            );
            progressed = true;
        }

        return new ActivityPassOutcome(
            steps,
            progressed,
            WaitingOrSuspended: false,
            FailedError: null
        );
    }

    private async Task<SingleActionOutcome> ExecuteSingleActionAsync(
        WorkflowInstance instance,
        WorkflowType definition,
        WorkflowActivity activity,
        WorkflowAction action,
        Dictionary<WorkflowActionTypeId, WorkflowActionType> actionLookup,
        CancellationToken ct
    )
    {
        if (!_pluginMap.TryGetValue(action.ActionPluginKey, out var plugin))
        {
            instance.RecordExecutionFailure(
                $"Missing workflow action plugin '{action.ActionPluginKey}'.",
                definition.MaxProcessingAttempts,
                definition.ProcessingIntervalSeconds,
                clock.UtcNow
            );
            return new SingleActionOutcome(
                Paused: false,
                HaltActivity: true,
                WorkflowErrors.ActionPluginNotFound
            );
        }

        _ = actionLookup.TryGetValue(action.WorkflowActionTypeId, out var actionDef);
        var execContext = CreateExecutionContext(
            instance,
            activity,
            action,
            actionDef,
            clock.UtcNow
        );
        action.MarkProcessed(execContext.NowUtc);

        var execRes = await plugin.ExecuteAsync(execContext, ct);
        if (execRes.IsFailure)
        {
            instance.RecordExecutionFailure(
                execRes.PrimaryError.Description,
                definition.MaxProcessingAttempts,
                definition.ProcessingIntervalSeconds,
                clock.UtcNow
            );
            return new SingleActionOutcome(Paused: false, HaltActivity: true, execRes.PrimaryError);
        }

        var halt = WorkflowDirectiveApplier.Apply(
            dbContext,
            instance,
            definition,
            activity,
            action,
            actionDef,
            execRes.Value,
            clock.UtcNow,
            out var paused
        );
        return new SingleActionOutcome(paused, halt, FailedError: null);
    }

    private void FinishIdlePass(
        WorkflowInstance instance,
        WorkflowType definition,
        bool waitingOrSuspended
    )
    {
        if (waitingOrSuspended)
        {
            instance.PauseForUserInput(clock.UtcNow);
        }
        else
        {
            instance.ScheduleNextPass(definition.ProcessingIntervalSeconds, clock.UtcNow);
        }
    }

    private static bool IsReadyForProcessing(WorkflowInstance instance, DateTimeOffset now)
    {
        return !instance.IsTerminal
            && (
                instance.Status != WorkflowInstanceStatus.Suspended
                || !instance.NextScheduledProcessAtUtc.HasValue
                || instance.NextScheduledProcessAtUtc.Value <= now
            );
    }

    private static WorkflowActionExecutionContext CreateExecutionContext(
        WorkflowInstance instance,
        WorkflowActivity activity,
        WorkflowAction action,
        WorkflowActionType? actionDef,
        DateTimeOffset now
    )
    {
        return new WorkflowActionExecutionContext(
            instance.Id.Value,
            instance.WorkflowTypeId.Value,
            instance.WorkflowNumber,
            instance.Name,
            activity.Id.Value,
            activity.Key,
            action.Id.Value,
            action.WorkflowActionTypeId.Value,
            action.Key,
            instance.StateLabel,
            instance.InitiatorPersonId,
            activity.AssignedPersonId,
            activity.AssignedGroupId,
            instance.PrimaryEntityTypeKey,
            instance.PrimaryEntityId,
            actionDef?.Configuration
                ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            instance.Attributes,
            activity.Attributes,
            now
        );
    }

    private sealed record SweepOutcome(
        int UpdatedStepCount,
        bool MadeProgress,
        bool WaitingOrSuspended,
        DomainError? FailedError
    );

    private sealed record ActivityPassOutcome(
        int StepsRun,
        bool MadeProgress,
        bool WaitingOrSuspended,
        DomainError? FailedError
    );

    private sealed record SingleActionOutcome(
        bool Paused,
        bool HaltActivity,
        DomainError? FailedError
    );
}
