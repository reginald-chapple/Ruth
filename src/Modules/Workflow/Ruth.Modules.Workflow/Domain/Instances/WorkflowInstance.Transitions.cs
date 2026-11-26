using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain.Instances;

public sealed partial class WorkflowInstance
{
    public void PauseForUserInput(DateTimeOffset utcNow)
    {
        if (!IsTerminal)
        {
            Status = WorkflowInstanceStatus.WaitingForInput;
            LastProcessedAtUtc = utcNow.ToUniversalTime();
            NextScheduledProcessAtUtc = null;
        }
    }

    public void SuspendUntil(DateTimeOffset resumeAtUtc, DateTimeOffset utcNow)
    {
        if (!IsTerminal)
        {
            Status = WorkflowInstanceStatus.Suspended;
            LastProcessedAtUtc = utcNow.ToUniversalTime();
            NextScheduledProcessAtUtc = resumeAtUtc.ToUniversalTime();
        }
    }

    public void ResumeProcessing(DateTimeOffset utcNow)
    {
        if (!IsTerminal)
        {
            Status = WorkflowInstanceStatus.Active;
            LastProcessedAtUtc = utcNow.ToUniversalTime();
            ProcessingAttemptCount++;
        }
    }

    public void ScheduleNextPass(int intervalSeconds, DateTimeOffset utcNow)
    {
        if (!IsTerminal && Status == WorkflowInstanceStatus.Active)
        {
            NextScheduledProcessAtUtc = utcNow
                .ToUniversalTime()
                .AddSeconds(Math.Max(1, intervalSeconds));
        }
    }

    public Result Complete(
        string summary,
        WorkflowLoggingLevel loggingLevel,
        DateTimeOffset utcNow,
        Guid? actorPersonId = null
    )
    {
        if (IsTerminal)
        {
            return Result.Failure(WorkflowErrors.WorkflowAlreadyClosed);
        }

        var normalizedNow = utcNow.ToUniversalTime();
        foreach (var openAct in _activities.Where(static a => !a.IsCompleted))
        {
            openAct.MarkCompleted(normalizedNow);
        }

        Status = WorkflowInstanceStatus.Completed;
        if (string.Equals(StateLabel, "Active", StringComparison.OrdinalIgnoreCase))
        {
            StateLabel = "Completed";
        }

        LastProcessedAtUtc = normalizedNow;
        CompletedAtUtc = normalizedNow;
        NextScheduledProcessAtUtc = null;
        ErrorMessage = null;
        AddLog(loggingLevel, WorkflowLogKind.Lifecycle, summary, normalizedNow, actorPersonId);

        RaiseDomainEvent(
            new WorkflowCompletedDomainEvent(
                Id.Value,
                WorkflowTypeId.Value,
                WorkflowNumber,
                Name,
                StateLabel,
                InitiatorPersonId,
                PrimaryEntityTypeKey,
                PrimaryEntityId,
                normalizedNow
            )
        );

        return Result.Success();
    }

    public Result Cancel(
        string reason,
        WorkflowLoggingLevel loggingLevel,
        DateTimeOffset utcNow,
        Guid? actorPersonId = null
    )
    {
        if (IsTerminal)
        {
            return Result.Failure(WorkflowErrors.WorkflowAlreadyClosed);
        }

        var normalizedNow = utcNow.ToUniversalTime();
        Status = WorkflowInstanceStatus.Cancelled;
        StateLabel = "Cancelled";
        CompletedAtUtc = normalizedNow;
        NextScheduledProcessAtUtc = null;
        AddLog(
            loggingLevel,
            WorkflowLogKind.Lifecycle,
            $"Cancelled: {reason}",
            normalizedNow,
            actorPersonId
        );
        return Result.Success();
    }

    public void RecordExecutionFailure(
        string error,
        int maxAttempts,
        int retryDelaySeconds,
        DateTimeOffset utcNow
    )
    {
        var normalizedNow = utcNow.ToUniversalTime();
        ErrorMessage = WorkflowTextNormalizer.TrimOrNull(error, 1000) ?? "Action failed.";
        _logs.Add(WorkflowLog.Create(Id, WorkflowLogKind.Error, ErrorMessage, normalizedNow));

        if (ProcessingAttemptCount >= Math.Max(1, maxAttempts))
        {
            Status = WorkflowInstanceStatus.Faulted;
            StateLabel = "Faulted";
            CompletedAtUtc = normalizedNow;
            NextScheduledProcessAtUtc = null;
        }
        else
        {
            NextScheduledProcessAtUtc = normalizedNow.AddSeconds(Math.Max(5, retryDelaySeconds));
        }
    }

    public void AddLog(
        WorkflowLoggingLevel configuredLevel,
        WorkflowLogKind kind,
        string summary,
        DateTimeOffset utcNow,
        Guid? actorPersonId = null
    )
    {
        var shouldRecord = kind switch
        {
            WorkflowLogKind.Error => true,
            WorkflowLogKind.Lifecycle or WorkflowLogKind.FormSubmission => configuredLevel
                >= WorkflowLoggingLevel.Workflow,
            WorkflowLogKind.Activity => configuredLevel >= WorkflowLoggingLevel.Activity,
            WorkflowLogKind.Action or WorkflowLogKind.AttributeChange or _ => configuredLevel
                >= WorkflowLoggingLevel.Action,
        };

        if (shouldRecord)
        {
            _logs.Add(WorkflowLog.Create(Id, kind, summary, utcNow, actorPersonId));
        }
    }
}
