using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Scheduling;

/// <summary>
/// Executes a single <see cref="IScheduledJob" /> under a PostgreSQL advisory lock with
/// root <see cref="Activity" /> isolation, isolated status/history bookkeeping, and history pruning.
/// </summary>
public sealed class ScheduledJobExecutor
{
    private readonly PostgresAdvisoryLockProvider _lockProvider;
    private readonly IClock _clock;
    private readonly ILogger<ScheduledJobExecutor> _logger;
    private readonly ScheduledJobBookkeeper _bookkeeper;

    /// <summary>Initializes a new <see cref="ScheduledJobExecutor" />.</summary>
    public ScheduledJobExecutor(
        IServiceScopeFactory scopeFactory,
        PostgresAdvisoryLockProvider lockProvider,
        IClock clock,
        IOptions<SchedulerOptions> options,
        ILogger<ScheduledJobExecutor> logger
    )
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(lockProvider);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _lockProvider = lockProvider;
        _clock = clock;
        _logger = logger;
        _bookkeeper = new ScheduledJobBookkeeper(scopeFactory, options.Value, logger);
    }

    /// <summary>
    /// Attempts to acquire the distributed lock for <paramref name="job" /> and execute it once.
    /// Returns <see langword="false" /> when skipped due to lock contention.
    /// </summary>
    public Task<bool> ExecuteAsync(IScheduledJob job, CancellationToken cancellationToken = default)
    {
        return ExecuteCoreAsync(job, requireDueAndActive: false, cancellationToken);
    }

    /// <summary>
    /// Attempts to acquire the distributed lock for <paramref name="job" />, re-verifies that it is
    /// still active and due, and executes it once. Returns <see langword="false" /> when skipped.
    /// </summary>
    public Task<bool> ExecuteIfDueAsync(
        IScheduledJob job,
        CancellationToken cancellationToken = default
    )
    {
        return ExecuteCoreAsync(job, requireDueAndActive: true, cancellationToken);
    }

    private async Task<bool> ExecuteCoreAsync(
        IScheduledJob job,
        bool requireDueAndActive,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(job);

        var lockHandle = await _lockProvider.TryAcquireAsync(job.JobKey, cancellationToken);
        if (lockHandle is null)
        {
            SchedulerLog.LockContentionSkipped(_logger, job.JobKey);
            SchedulerDiagnostics.JobsSkippedContention.Add(1);
            return false;
        }

        try
        {
            return await ExecuteLockedAsync(job, requireDueAndActive, cancellationToken);
        }
        finally
        {
            await lockHandle.DisposeAsync();
        }
    }

    private async Task<bool> ExecuteLockedAsync(
        IScheduledJob job,
        bool requireDueAndActive,
        CancellationToken cancellationToken
    )
    {
        var startedUtc = _clock.UtcNow;
        var historyId = StronglyTypedId.NextGuid(startedUtc);
        var beginResult = await _bookkeeper.TryBeginRunAsync(
            job.JobKey,
            historyId,
            startedUtc,
            requireDueAndActive
        );

        if (!beginResult.ShouldExecute)
        {
            SchedulerLog.NotDueOrInactiveSkipped(_logger, job.JobKey);
            return false;
        }

        var priorActivity = Activity.Current;
        Activity.Current = null;

        try
        {
            using var activity = SchedulerDiagnostics.ActivitySource.StartActivity(
                $"job:{job.JobKey}",
                ActivityKind.Internal
            );
            await RunWithBookkeepingAsync(
                job,
                beginResult,
                historyId,
                startedUtc,
                cancellationToken
            );
            return true;
        }
        finally
        {
            Activity.Current = priorActivity;
        }
    }

    private async Task RunWithBookkeepingAsync(
        IScheduledJob job,
        BeginRunResult beginResult,
        Guid historyId,
        DateTimeOffset startedUtc,
        CancellationToken cancellationToken
    )
    {
        var startTimestamp = _clock.GetTimestamp();
        OperationCanceledException? canceledException = null;
        JobExecutionResult outcome;

        try
        {
            outcome = await InvokeJobSafelyAsync(job, cancellationToken);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            canceledException = ex;
            outcome = JobExecutionResult.Failed(FormatExceptionMessage(ex));
        }

        var durationMs = Math.Max(
            0L,
            (long)_clock.GetElapsedTime(startTimestamp).TotalMilliseconds
        );
        var completedUtc = _clock.UtcNow;

        await PersistOutcomeAsync(
            job,
            beginResult.JobId,
            beginResult.CronExpression,
            beginResult.HistoryCount,
            historyId,
            startedUtc,
            completedUtc,
            durationMs,
            outcome
        );

        if (canceledException is not null)
        {
            ExceptionDispatchInfo.Throw(canceledException);
        }
    }

    private async Task PersistOutcomeAsync(
        IScheduledJob job,
        Guid? jobId,
        string? cronExpr,
        int historyCount,
        Guid historyId,
        DateTimeOffset startedUtc,
        DateTimeOffset completedUtc,
        long durationMs,
        JobExecutionResult outcome
    )
    {
        SchedulerOutcomeHelper.RecordMetricsAndLog(_logger, job.JobKey, outcome);

        var nextRunUtc = ScheduledJobBookkeeper.ComputeNextRunUtc(
            job.JobKey,
            cronExpr ?? job.DefaultCronExpression,
            completedUtc,
            _clock.LocalTimeZone,
            _logger
        );
        await _bookkeeper.TryUpdateJobStatusAsync(
            job.JobKey,
            outcome,
            completedUtc,
            durationMs,
            nextRunUtc
        );

        if (jobId.HasValue)
        {
            await _bookkeeper.TryCompleteAndPruneHistoryAsync(
                jobId.Value,
                job.JobKey,
                historyId,
                startedUtc,
                completedUtc,
                durationMs,
                outcome,
                historyCount
            );
        }
    }

    private async Task<JobExecutionResult> InvokeJobSafelyAsync(
        IScheduledJob job,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await job.ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = FormatExceptionMessage(ex);
            SchedulerLog.JobFailed(_logger, ex, job.JobKey, message);
            return JobExecutionResult.Failed(message);
        }
    }

    internal static string FormatExceptionMessage(Exception exception)
    {
        return SchedulerOutcomeHelper.FormatExceptionMessage(exception);
    }
}
