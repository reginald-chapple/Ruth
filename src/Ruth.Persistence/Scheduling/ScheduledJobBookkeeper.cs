using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Jobs;

namespace Ruth.Persistence.Scheduling;

internal sealed class ScheduledJobBookkeeper(
    IServiceScopeFactory scopeFactory,
    SchedulerOptions options,
    ILogger<ScheduledJobExecutor> logger
)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly SchedulerOptions _options = options;
    private readonly ILogger<ScheduledJobExecutor> _logger = logger;

    public async Task<BeginRunResult> TryBeginRunAsync(
        string jobKey,
        Guid historyId,
        DateTimeOffset startedUtc,
        bool requireDueAndActive
    )
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

            var jobRow = await db.ScheduledJobs.FirstOrDefaultAsync(
                x => x.JobKey == jobKey,
                CancellationToken.None
            );

            if (jobRow is null)
            {
                return requireDueAndActive
                    ? BeginRunResult.Skip(_options.DefaultHistoryCount)
                    : BeginRunResult.ProceedWithoutRecord(_options.DefaultHistoryCount);
            }

            if (requireDueAndActive && !IsDueAndActive(jobRow, startedUtc))
            {
                return BeginRunResult.Skip(_options.DefaultHistoryCount);
            }

            jobRow.MarkRunning(startedUtc, _options.WorkerId);
            _ = db.ScheduledJobHistories.Add(
                ScheduledJobHistory.Start(historyId, jobRow.Id, _options.WorkerId, startedUtc)
            );
            _ = await db.SaveChangesAsync(CancellationToken.None);

            return new BeginRunResult(
                ShouldExecute: true,
                jobRow.Id,
                jobRow.CronExpression,
                jobRow.HistoryCount
            );
        }
        catch (Exception ex)
        {
            SchedulerLog.BookkeepingFailed(_logger, ex, jobKey, "BeginRun");
            return requireDueAndActive
                ? BeginRunResult.Skip(_options.DefaultHistoryCount)
                : BeginRunResult.ProceedWithoutRecord(_options.DefaultHistoryCount);
        }
    }

    private static bool IsDueAndActive(ScheduledJob jobRow, DateTimeOffset startedUtc)
    {
        return jobRow.IsActive && jobRow.NextRunUtc is { } nextRunUtc && nextRunUtc <= startedUtc;
    }

    public async Task TryUpdateJobStatusAsync(
        string jobKey,
        JobExecutionResult outcome,
        DateTimeOffset completedUtc,
        long durationMs,
        DateTimeOffset? nextRunUtc
    )
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            var jobRow = await db.ScheduledJobs.FirstOrDefaultAsync(
                x => x.JobKey == jobKey,
                CancellationToken.None
            );

            if (jobRow is null)
            {
                return;
            }

            jobRow.RecordOutcome(
                outcome.Status,
                outcome.Message,
                completedUtc,
                durationMs,
                _options.WorkerId,
                nextRunUtc
            );
            _ = await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            SchedulerLog.BookkeepingFailed(_logger, ex, jobKey, "UpdateJobStatus");
        }
    }

    public async Task TryCompleteAndPruneHistoryAsync(
        Guid scheduledJobId,
        string jobKey,
        Guid historyId,
        DateTimeOffset startedUtc,
        DateTimeOffset completedUtc,
        long durationMs,
        JobExecutionResult outcome,
        int configuredHistoryCount
    )
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

            var history = await db.ScheduledJobHistories.FirstOrDefaultAsync(
                x => x.Id == historyId,
                CancellationToken.None
            );

            if (history is null)
            {
                _ = db.ScheduledJobHistories.Add(
                    ScheduledJobHistory.CreateCompleted(
                        historyId,
                        scheduledJobId,
                        _options.WorkerId,
                        startedUtc,
                        completedUtc,
                        durationMs,
                        outcome.Status,
                        outcome.Message
                    )
                );
            }
            else
            {
                history.Complete(completedUtc, durationMs, outcome.Status, outcome.Message);
            }

            _ = await db.SaveChangesAsync(CancellationToken.None);
            await PruneHistoryAsync(db, scheduledJobId, configuredHistoryCount);
        }
        catch (Exception ex)
        {
            SchedulerLog.BookkeepingFailed(_logger, ex, jobKey, "CompleteAndPruneHistory");
        }
    }

    private async Task PruneHistoryAsync(
        SchedulerDbContext db,
        Guid scheduledJobId,
        int configuredHistoryCount
    )
    {
        var fallbackCount = _options.DefaultHistoryCount <= 0 ? 500 : _options.DefaultHistoryCount;
        var effectiveCount = configuredHistoryCount <= 0 ? fallbackCount : configuredHistoryCount;

        var overflowIdsQuery = db
            .ScheduledJobHistories.Where(h => h.ScheduledJobId == scheduledJobId)
            .OrderByDescending(h => h.StartedOnUtc)
            .ThenByDescending(h => h.Id)
            .Skip(effectiveCount)
            .Select(h => h.Id);

        _ = await db
            .ScheduledJobHistories.Where(h => overflowIdsQuery.Contains(h.Id))
            .ExecuteDeleteAsync(CancellationToken.None);
    }

    public static async Task CompleteOrphanedHistoriesAsync(
        SchedulerDbContext db,
        Guid scheduledJobId,
        DateTimeOffset nowUtc,
        string orphanMessage,
        CancellationToken cancellationToken
    )
    {
        var orphanedHistories = await db
            .ScheduledJobHistories.Where(h =>
                h.ScheduledJobId == scheduledJobId && h.Status == ScheduledJobStatus.Running
            )
            .ToListAsync(cancellationToken);

        foreach (var history in orphanedHistories)
        {
            var durationMs = Math.Max(0L, (long)(nowUtc - history.StartedOnUtc).TotalMilliseconds);
            history.Complete(nowUtc, durationMs, JobExecutionStatus.Failed, orphanMessage);
        }
    }

    public static Dictionary<string, IScheduledJob> BuildJobMap(IEnumerable<IScheduledJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        var map = new Dictionary<string, IScheduledJob>(StringComparer.Ordinal);
        foreach (var job in jobs)
        {
            if (!map.TryAdd(job.JobKey, job))
            {
                throw new InvalidOperationException($"Duplicate scheduled job key '{job.JobKey}'.");
            }
        }

        return map;
    }

    public static DateTimeOffset? ComputeNextRunUtc(
        string jobKey,
        string cronExpressionText,
        DateTimeOffset fromUtc,
        TimeZoneInfo timeZone,
        ILogger logger
    )
    {
        if (!CronExpression.TryParse(cronExpressionText, out var parsed))
        {
            SchedulerLog.InvalidCronExpression(logger, jobKey, cronExpressionText);
            return null;
        }

        return parsed.GetNextOccurrence(fromUtc, timeZone);
    }
}
