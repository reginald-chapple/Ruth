using Ruth.SharedKernel.Jobs;

namespace Ruth.Persistence.Scheduling;

/// <summary>Persisted schedule metadata and last execution state for an <see cref="IScheduledJob" />.</summary>
public sealed class ScheduledJob
{
    private ScheduledJob() { }

    /// <summary>Gets the UUIDv7 primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the stable unique job key matching <see cref="IScheduledJob.JobKey" />.</summary>
    public string JobKey { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable job name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets an optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the active cron expression.</summary>
    public string CronExpression { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the job is enabled for automatic execution.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets a value indicating whether this is a built-in system job.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets the UTC timestamp of the most recent run start or completion.</summary>
    public DateTimeOffset? LastRunUtc { get; private set; }

    /// <summary>Gets the UTC timestamp of the most recent <see cref="JobExecutionStatus.Success" /> run.</summary>
    public DateTimeOffset? LastSuccessfulRunUtc { get; private set; }

    /// <summary>Gets the duration of the most recent run in milliseconds.</summary>
    public long? LastDurationMs { get; private set; }

    /// <summary>Gets the status string of the most recent run.</summary>
    public string? LastStatus { get; private set; }

    /// <summary>Gets the diagnostic message from the most recent run.</summary>
    public string? LastStatusMessage { get; private set; }

    /// <summary>Gets the worker identifier that most recently executed this job.</summary>
    public string? LastRunWorkerId { get; private set; }

    /// <summary>Gets the maximum number of <see cref="ScheduledJobHistory" /> rows retained (defaults to 500 when &lt;= 0).</summary>
    public int HistoryCount { get; private set; } = 500;

    /// <summary>Gets the next scheduled UTC execution timestamp, or <see langword="null" />.</summary>
    public DateTimeOffset? NextRunUtc { get; private set; }

    /// <summary>Gets the PostgreSQL xmin optimistic concurrency token.</summary>
    public uint Version { get; private set; }

    /// <summary>Creates a new <see cref="ScheduledJob" /> record.</summary>
    public static ScheduledJob Create(
        Guid id,
        string jobKey,
        string name,
        string cronExpression,
        DateTimeOffset? nextRunUtc,
        int historyCount = 500,
        bool isActive = true,
        bool isSystem = true,
        string? description = null
    )
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("ScheduledJob ID cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(jobKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

        return new ScheduledJob
        {
            Id = id,
            JobKey = jobKey,
            Name = name,
            Description = description,
            CronExpression = cronExpression,
            IsActive = isActive,
            IsSystem = isSystem,
            HistoryCount = historyCount <= 0 ? 500 : historyCount,
            NextRunUtc = nextRunUtc?.ToUniversalTime(),
        };
    }

    /// <summary>Updates the job state to <see cref="ScheduledJobStatus.Running" />.</summary>
    public void MarkRunning(DateTimeOffset startedOnUtc, string workerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

        LastStatus = ScheduledJobStatus.Running;
        LastStatusMessage = null;
        LastRunUtc = startedOnUtc.ToUniversalTime();
        LastRunWorkerId = workerId;
    }

    /// <summary>Updates the job state after completion, preserving <see cref="LastSuccessfulRunUtc" /> on non-success.</summary>
    public void RecordOutcome(
        JobExecutionStatus status,
        string? message,
        DateTimeOffset completedOnUtc,
        long durationMs,
        string workerId,
        DateTimeOffset? nextRunUtc
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

        var utcCompleted = completedOnUtc.ToUniversalTime();
        LastRunUtc = utcCompleted;
        LastDurationMs = durationMs;
        LastRunWorkerId = workerId;
        LastStatusMessage = message;
        NextRunUtc = nextRunUtc?.ToUniversalTime();

        LastStatus = status switch
        {
            JobExecutionStatus.Success => ScheduledJobStatus.Success,
            JobExecutionStatus.Warning => ScheduledJobStatus.Warning,
            JobExecutionStatus.Failed => ScheduledJobStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: null),
        };

        if (status == JobExecutionStatus.Success)
        {
            LastSuccessfulRunUtc = utcCompleted;
        }
    }

    /// <summary>Updates the next scheduled UTC run time.</summary>
    public void SetNextRunUtc(DateTimeOffset? nextRunUtc)
    {
        NextRunUtc = nextRunUtc?.ToUniversalTime();
    }

    /// <summary>Recovers an orphaned <see cref="ScheduledJobStatus.Running" /> state on startup.</summary>
    public void MarkOrphanedFailure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        LastStatus = ScheduledJobStatus.Failed;
        LastStatusMessage = message;
    }
}
