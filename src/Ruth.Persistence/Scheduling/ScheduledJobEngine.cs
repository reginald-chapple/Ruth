using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Scheduling;

/// <summary>
/// Hosted background service managing scheduled <see cref="IScheduledJob" /> registration sync,
/// advisory-lock-gated orphan recovery, cron polling, and manual Run-Now triggers.
/// </summary>
public sealed class ScheduledJobEngine : BackgroundService
{
    private readonly Dictionary<string, IScheduledJob> _jobs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ScheduledJobExecutor _executor;
    private readonly IClock _clock;
    private readonly SchedulerOptions _options;
    private readonly ILogger<ScheduledJobEngine> _logger;
    private readonly ScheduledJobSynchronizer _synchronizer;

    /// <summary>Initializes a new <see cref="ScheduledJobEngine" />.</summary>
    public ScheduledJobEngine(
        IEnumerable<IScheduledJob> jobs,
        IServiceScopeFactory scopeFactory,
        PostgresAdvisoryLockProvider lockProvider,
        ScheduledJobExecutor executor,
        IClock clock,
        IOptions<SchedulerOptions> options,
        ILogger<ScheduledJobEngine> logger
    )
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(lockProvider);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _jobs = ScheduledJobBookkeeper.BuildJobMap(jobs);
        _scopeFactory = scopeFactory;
        _executor = executor;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
        _synchronizer = new ScheduledJobSynchronizer(
            _jobs,
            scopeFactory,
            lockProvider,
            clock,
            _options,
            logger
        );
    }

    /// <summary>
    /// Synchronizes registered <see cref="IScheduledJob" /> definitions into scheduler.scheduled_jobs
    /// and safely recovers orphaned <see cref="ScheduledJobStatus.Running" /> rows under advisory lock.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_jobs.Count == 0)
        {
            return;
        }

        await _synchronizer.SyncRegisteredJobsAsync(cancellationToken);
        await _synchronizer.RecoverOrphanedRunningJobsAsync(cancellationToken);
    }

    /// <summary>
    /// Manually triggers a registered job immediately through the same distributed lock and bookkeeping pipeline.
    /// </summary>
    public Task<bool> TriggerJobNowAsync(
        string jobKey,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobKey);

        return _jobs.TryGetValue(jobKey, out var job)
            ? _executor.ExecuteAsync(job, cancellationToken)
            : throw new KeyNotFoundException($"No scheduled job registered with key '{jobKey}'.");
    }

    /// <summary>Runs one polling sweep of due active jobs.</summary>
    public async Task<int> RunDueJobsOnceAsync(CancellationToken cancellationToken = default)
    {
        if (_jobs.Count == 0)
        {
            return 0;
        }

        var nowUtc = _clock.UtcNow;
        List<string> dueKeys;

        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            dueKeys = await db
                .ScheduledJobs.AsNoTracking()
                .Where(j => j.IsActive && j.NextRunUtc != null && j.NextRunUtc <= nowUtc)
                .OrderBy(j => j.NextRunUtc)
                .Select(j => j.JobKey)
                .ToListAsync(cancellationToken);
        }

        var executedCount = 0;
        foreach (var jobKey in dueKeys)
        {
            if (
                _jobs.TryGetValue(jobKey, out var job)
                && await _executor.ExecuteIfDueAsync(job, cancellationToken)
            )
            {
                executedCount++;
            }
        }

        return executedCount;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_jobs.Count == 0)
        {
            return;
        }

        await Task.Yield();
        var initialized = await TryInitializeAsync(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var timer = new PeriodicTimer(_options.PollingInterval, _clock.TimeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                initialized = await RunTickAsync(initialized, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown cancellation.
        }
    }

    private async Task<bool> TryInitializeAsync(CancellationToken stoppingToken)
    {
        try
        {
            await InitializeAsync(stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            SchedulerLog.InitializationFailed(_logger, ex);
            return false;
        }
    }

    private async Task<bool> RunTickAsync(bool initialized, CancellationToken stoppingToken)
    {
        try
        {
            if (!initialized)
            {
                await InitializeAsync(stoppingToken);
            }

            _ = await RunDueJobsOnceAsync(stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return initialized;
        }
        catch (Exception ex)
        {
            if (!initialized)
            {
                SchedulerLog.InitializationFailed(_logger, ex);
            }
            else
            {
                SchedulerLog.EngineSweepFailed(_logger, ex);
            }

            return initialized;
        }
    }
}
