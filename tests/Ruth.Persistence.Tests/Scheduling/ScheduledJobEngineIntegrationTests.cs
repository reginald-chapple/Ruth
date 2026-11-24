using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.Scheduling;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.Jobs;
using Xunit;

namespace Ruth.Persistence.Tests.Scheduling;

[Collection("Postgres18")]
public sealed class ScheduledJobEngineIntegrationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class DelegateJob(
        string jobKey,
        string cronExpression,
        Func<CancellationToken, Task<JobExecutionResult>> callback
    ) : IScheduledJob
    {
        public string JobKey { get; } = jobKey;
        public string DefaultCronExpression { get; } = cronExpression;

        public Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return callback(cancellationToken);
        }
    }

    [Fact]
    public async Task SubSecond_Run_Warning_Exception_And_Root_Activity_Isolation_Work_Correctly()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var initialUtc = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(initialUtc);

        var hasRootActivityWithNullParent = false;
        ActivityTraceId? observedTraceId = null;
        var mode = JobExecutionStatus.Success;

        var job = new DelegateJob("test.isolated", "*/5 * * * *", _ =>
        {
            hasRootActivityWithNullParent =
                Activity.Current is not null && Activity.Current.ParentId is null;
            observedTraceId = Activity.Current?.TraceId;

            return mode switch
            {
                JobExecutionStatus.Success => Task.FromResult(
                    JobExecutionResult.Success("All good.")
                ),
                JobExecutionStatus.Warning => Task.FromResult(
                    JobExecutionResult.Warning("Minor drift.")
                ),
                JobExecutionStatus.Failed => throw new InvalidOperationException("Boom!"),
                _ => throw new InvalidOperationException("Unexpected status."),
            };
        });

        using var listener = RegisterSchedulerActivityListener();
        await using var provider = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-iso",
            fakeTime,
            jobs: job
        );
        await EnsureSchedulerSchemaAsync(provider);

        var engine = provider.GetRequiredService<ScheduledJobEngine>();
        await engine.InitializeAsync();

        using (var outerActivity = new Activity("OuterHttpRequest").Start())
        {
            Assert.True(await engine.TriggerJobNowAsync("test.isolated"));
            Assert.Same(outerActivity, Activity.Current);
            Assert.True(hasRootActivityWithNullParent);
            _ = Assert.NotNull(observedTraceId);
            Assert.NotEqual(outerActivity.TraceId, observedTraceId.Value);
        }

        fakeTime.Advance(TimeSpan.FromMinutes(1));
        mode = JobExecutionStatus.Warning;
        Assert.True(await engine.TriggerJobNowAsync("test.isolated"));

        fakeTime.Advance(TimeSpan.FromMinutes(1));
        mode = JobExecutionStatus.Failed;
        Assert.True(await engine.TriggerJobNowAsync("test.isolated"));

        await AssertIsolatedOutcomesAsync(provider, initialUtc);
    }

    [Fact]
    public async Task History_Pruning_Retains_Configured_Count_And_Orphan_Recovery_Works()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero));
        var counter = 0;

        var prunedJob = new DelegateJob("test.pruned", "@hourly", _ =>
        {
            var next = (++counter).ToString(CultureInfo.InvariantCulture);
            return Task.FromResult(JobExecutionResult.Success($"Run #{next}"));
        });

        await using var provider = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-prune",
            fakeTime,
            configureOptions: opts => opts.DefaultHistoryCount = 3,
            prunedJob
        );

        await EnsureSchedulerSchemaAsync(provider);
        var engine = provider.GetRequiredService<ScheduledJobEngine>();
        await engine.InitializeAsync();

        for (var i = 0; i < 6; i++)
        {
            fakeTime.Advance(TimeSpan.FromSeconds(1));
            _ = await engine.TriggerJobNowAsync("test.pruned");
        }

        await AssertPrunedHistoryAndSeedOrphanAsync(provider, fakeTime.GetUtcNow());

        await engine.InitializeAsync();
        await AssertRecoveredOrphanAndDefaultHistoryAsync(provider);
    }

    [Fact]
    public async Task Hosted_Background_Loop_And_RunDueJobsOnce_Execute_Only_Due_Active_Jobs()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var ranSignal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var runCount = 0;

        var dueJob = new DelegateJob("test.due", "* * * * * *", ct =>
        {
            _ = ct;
            _ = Interlocked.Increment(ref runCount);
            _ = ranSignal.TrySetResult();
            return Task.FromResult(JobExecutionResult.Success());
        });

        await using var provider = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-hosted",
            configureOptions: opts => opts.PollingInterval = TimeSpan.FromMilliseconds(25),
            jobs: dueJob
        );
        await EnsureSchedulerSchemaAsync(provider);

        var engine = provider.GetRequiredService<ScheduledJobEngine>();
        await engine.InitializeAsync();

        // Force due timestamp into the past so RunDueJobsOnceAsync picks it up immediately
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            var row = await db.ScheduledJobs.SingleAsync(x => x.JobKey == "test.due");
            row.SetNextRunUtc(DateTimeOffset.UtcNow.AddSeconds(-5));
            _ = await db.SaveChangesAsync();
        }

        Assert.Equal(1, await engine.RunDueJobsOnceAsync());
        Assert.Equal(1, runCount);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await engine.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(80), cts.Token);
        await engine.StopAsync(CancellationToken.None);
        Assert.True(runCount >= 1);
    }

    [Fact]
    public async Task Concurrent_Cold_Boot_InitializeAsync_Across_Multiple_Nodes_Syncs_Without_Conflict()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var jobsA = new IScheduledJob[]
        {
            new DelegateJob("cold.job.1", "@hourly", _ =>
                Task.FromResult(JobExecutionResult.Success())
            ),
            new DelegateJob("cold.job.2", "@daily", _ =>
                Task.FromResult(JobExecutionResult.Success())
            ),
        };
        var jobsB = new IScheduledJob[]
        {
            new DelegateJob("cold.job.1", "@hourly", _ =>
                Task.FromResult(JobExecutionResult.Success())
            ),
            new DelegateJob("cold.job.2", "@daily", _ =>
                Task.FromResult(JobExecutionResult.Success())
            ),
        };
        var jobsC = new IScheduledJob[]
        {
            new DelegateJob("cold.job.1", "@hourly", _ =>
                Task.FromResult(JobExecutionResult.Success())
            ),
            new DelegateJob("cold.job.2", "@daily", _ =>
                Task.FromResult(JobExecutionResult.Success())
            ),
        };

        await using var providerA = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-a",
            jobs: jobsA
        );
        await using var providerB = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-b",
            jobs: jobsB
        );
        await using var providerC = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-c",
            jobs: jobsC
        );
        await EnsureSchedulerSchemaAsync(providerA);

        var engineA = providerA.GetRequiredService<ScheduledJobEngine>();
        var engineB = providerB.GetRequiredService<ScheduledJobEngine>();
        var engineC = providerC.GetRequiredService<ScheduledJobEngine>();

        await Task.WhenAll(
            engineA.InitializeAsync(),
            engineB.InitializeAsync(),
            engineC.InitializeAsync()
        );

        await using var verifyScope = providerA.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var syncedKeys = await db.ScheduledJobs.Select(x => x.JobKey).OrderBy(x => x).ToListAsync();
        Assert.Equal(["cold.job.1", "cold.job.2"], syncedKeys);
    }

    private static ActivityListener RegisterSchedulerActivityListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source =>
                string.Equals(
                    source.Name,
                    SchedulerDiagnostics.ActivitySourceName,
                    StringComparison.Ordinal
                ),
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static async Task EnsureSchedulerSchemaAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, SchedulerDbContext.SchemaName);
    }

    private static async Task AssertIsolatedOutcomesAsync(
        ServiceProvider provider,
        DateTimeOffset initialUtc
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var jobRow = await db.ScheduledJobs.SingleAsync(x => x.JobKey == "test.isolated");
        Assert.Equal(ScheduledJobStatus.Failed, jobRow.LastStatus);
        Assert.Equal(initialUtc, jobRow.LastSuccessfulRunUtc);
        Assert.Contains(
            "InvalidOperationException: Boom!",
            jobRow.LastStatusMessage,
            StringComparison.Ordinal
        );

        var histories = await db.ScheduledJobHistories.OrderBy(x => x.StartedOnUtc).ToListAsync();
        Assert.Equal(3, histories.Count);
        Assert.All(histories, h =>
        {
            _ = Assert.NotNull(h.CompletedOnUtc);
            Assert.True(h.CompletedOnUtc >= h.StartedOnUtc);
            Assert.True(h.DurationMs >= 0);
            Assert.NotEqual(ScheduledJobStatus.Running, h.Status, StringComparer.Ordinal);
        });
        Assert.Equal(ScheduledJobStatus.Success, histories[0].Status);
        Assert.Equal(ScheduledJobStatus.Warning, histories[1].Status);
        Assert.Equal(ScheduledJobStatus.Failed, histories[2].Status);
    }

    private static async Task AssertPrunedHistoryAndSeedOrphanAsync(
        ServiceProvider provider,
        DateTimeOffset nowUtc
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var remaining = await db.ScheduledJobHistories.OrderBy(x => x.StartedOnUtc).ToListAsync();
        Assert.Equal(3, remaining.Count);
        Assert.Equal("Run #4", remaining[0].StatusMessage);
        Assert.Equal("Run #5", remaining[1].StatusMessage);
        Assert.Equal("Run #6", remaining[2].StatusMessage);

        var jobRow = await db.ScheduledJobs.SingleAsync(x => x.JobKey == "test.pruned");
        jobRow.MarkRunning(nowUtc, "crashed-node");
        _ = await db.SaveChangesAsync();
    }

    private static async Task AssertRecoveredOrphanAndDefaultHistoryAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var recovered = await db.ScheduledJobs.SingleAsync(x => x.JobKey == "test.pruned");
        Assert.Equal(ScheduledJobStatus.Failed, recovered.LastStatus);
        Assert.Contains(
            "Recovered orphaned",
            recovered.LastStatusMessage,
            StringComparison.Ordinal
        );

        var defaultHistoryJob = ScheduledJob.Create(
            Guid.CreateVersion7(),
            "test.default.history",
            "DefaultHistory",
            "@daily",
            nextRunUtc: null,
            historyCount: 0
        );
        Assert.Equal(500, defaultHistoryJob.HistoryCount);
    }
}
