using Ruth.SharedKernel.Jobs;

namespace Ruth.Persistence.Scheduling;

/// <summary>Execution audit history row for a <see cref="ScheduledJob" /> run.</summary>
public sealed class ScheduledJobHistory
{
    private ScheduledJobHistory() { }

    /// <summary>Gets the UUIDv7 primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the owning <see cref="ScheduledJob.Id" />.</summary>
    public Guid ScheduledJobId { get; private set; }

    /// <summary>Gets the worker identifier that executed this run.</summary>
    public string WorkerId { get; private set; } = string.Empty;

    /// <summary>Gets the UTC start timestamp.</summary>
    public DateTimeOffset StartedOnUtc { get; private set; }

    /// <summary>Gets the UTC completion timestamp, or <see langword="null" /> while running.</summary>
    public DateTimeOffset? CompletedOnUtc { get; private set; }

    /// <summary>Gets the execution duration in milliseconds.</summary>
    public long? DurationMs { get; private set; }

    /// <summary>Gets the execution status string.</summary>
    public string Status { get; private set; } = ScheduledJobStatus.Running;

    /// <summary>Gets the diagnostic status message.</summary>
    public string? StatusMessage { get; private set; }

    /// <summary>Creates a new history row in the <see cref="ScheduledJobStatus.Running" /> state.</summary>
    public static ScheduledJobHistory Start(
        Guid id,
        Guid scheduledJobId,
        string workerId,
        DateTimeOffset startedOnUtc
    )
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("History ID cannot be empty.", nameof(id));
        }

        if (scheduledJobId == Guid.Empty)
        {
            throw new ArgumentException("ScheduledJob ID cannot be empty.", nameof(scheduledJobId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

        return new ScheduledJobHistory
        {
            Id = id,
            ScheduledJobId = scheduledJobId,
            WorkerId = workerId,
            StartedOnUtc = startedOnUtc.ToUniversalTime(),
            Status = ScheduledJobStatus.Running,
        };
    }

    /// <summary>Creates a completed history row directly (fallback when pre-run creation did not succeed).</summary>
    public static ScheduledJobHistory CreateCompleted(
        Guid id,
        Guid scheduledJobId,
        string workerId,
        DateTimeOffset startedOnUtc,
        DateTimeOffset completedOnUtc,
        long durationMs,
        JobExecutionStatus status,
        string? message
    )
    {
        var history = Start(id, scheduledJobId, workerId, startedOnUtc);
        history.Complete(completedOnUtc, durationMs, status, message);
        return history;
    }

    /// <summary>Transitions this history row to a terminal outcome.</summary>
    public void Complete(
        DateTimeOffset completedOnUtc,
        long durationMs,
        JobExecutionStatus status,
        string? message
    )
    {
        CompletedOnUtc = completedOnUtc.ToUniversalTime();
        DurationMs = durationMs;
        StatusMessage = message;
        Status = status switch
        {
            JobExecutionStatus.Success => ScheduledJobStatus.Success,
            JobExecutionStatus.Warning => ScheduledJobStatus.Warning,
            JobExecutionStatus.Failed => ScheduledJobStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: null),
        };
    }
}
