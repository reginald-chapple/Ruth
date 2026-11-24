using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Scheduling;

internal sealed class ScheduledJobSynchronizer(
    Dictionary<string, IScheduledJob> jobs,
    IServiceScopeFactory scopeFactory,
    PostgresAdvisoryLockProvider lockProvider,
    IClock clock,
    SchedulerOptions options,
    ILogger<ScheduledJobEngine> logger
)
{
    private const string SyncLockKey = "__ruth_scheduler_sync__";
    private const string OrphanRecoveryMessage =
        "Recovered orphaned running state after worker termination.";

    private readonly Dictionary<string, IScheduledJob> _jobs = jobs;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly PostgresAdvisoryLockProvider _lockProvider = lockProvider;
    private readonly IClock _clock = clock;
    private readonly SchedulerOptions _options = options;
    private readonly ILogger<ScheduledJobEngine> _logger = logger;

    public async Task SyncRegisteredJobsAsync(CancellationToken cancellationToken)
    {
        await using var syncLock = await AcquireSyncLockAsync(cancellationToken);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var nowUtc = _clock.UtcNow;

        var existingByKey = await db.ScheduledJobs.ToDictionaryAsync(
            x => x.JobKey,
            StringComparer.Ordinal,
            cancellationToken
        );

        foreach (var job in _jobs.Values)
        {
            if (existingByKey.TryGetValue(job.JobKey, out var existing))
            {
                EnsureNextRunPopulated(existing, job, nowUtc);
                continue;
            }

            _ = db.ScheduledJobs.Add(CreateScheduledJobEntity(job, nowUtc));
        }

        if (db.ChangeTracker.HasChanges())
        {
            _ = await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RecoverOrphanedRunningJobsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var stuckJobs = await db
            .ScheduledJobs.Where(x => x.LastStatus == ScheduledJobStatus.Running)
            .ToListAsync(cancellationToken);

        foreach (var stuckJob in stuckJobs)
        {
            await using var lockHandle = await _lockProvider.TryAcquireAsync(
                stuckJob.JobKey,
                cancellationToken
            );
            if (lockHandle is null)
            {
                continue;
            }

            stuckJob.MarkOrphanedFailure(OrphanRecoveryMessage);
            await ScheduledJobBookkeeper.CompleteOrphanedHistoriesAsync(
                db,
                stuckJob.Id,
                _clock.UtcNow,
                OrphanRecoveryMessage,
                cancellationToken
            );
            _ = await db.SaveChangesAsync(cancellationToken);
            SchedulerLog.RecoveredOrphanedJob(_logger, stuckJob.JobKey);
        }
    }

    private void EnsureNextRunPopulated(
        ScheduledJob existing,
        IScheduledJob job,
        DateTimeOffset nowUtc
    )
    {
        if (!existing.NextRunUtc.HasValue)
        {
            existing.SetNextRunUtc(
                ScheduledJobBookkeeper.ComputeNextRunUtc(
                    job.JobKey,
                    job.DefaultCronExpression,
                    nowUtc,
                    _clock.LocalTimeZone,
                    _logger
                )
            );
        }
    }

    private ScheduledJob CreateScheduledJobEntity(IScheduledJob job, DateTimeOffset nowUtc)
    {
        var nextRunUtc =
            job is IInitialSyncDueJob
                ? nowUtc
                : ScheduledJobBookkeeper.ComputeNextRunUtc(
                    job.JobKey,
                    job.DefaultCronExpression,
                    nowUtc,
                    _clock.LocalTimeZone,
                    _logger
                );

        return ScheduledJob.Create(
            StronglyTypedId.NextGuid(nowUtc),
            job.JobKey,
            job.JobKey,
            job.DefaultCronExpression,
            nextRunUtc,
            _options.DefaultHistoryCount
        );
    }

    private async Task<IAsyncDisposable> AcquireSyncLockAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var handle = await _lockProvider.TryAcquireAsync(SyncLockKey, cancellationToken);
            if (handle is not null)
            {
                return handle;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), TimeProvider.System, cancellationToken);
        }
    }
}
