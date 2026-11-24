using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.Scheduling;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.Jobs;
using Xunit;

namespace Ruth.Persistence.Tests.Scheduling;

[Collection("Postgres18")]
public sealed class ScheduledJobEngineToctouTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    [Fact]
    public async Task Staggered_RunDueJobsOnceAsync_Does_Not_Double_Execute_In_Same_Cron_Window()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var nowUtc = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(nowUtc);
        var job1Entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var job1Release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var race1Runs = 0;
        var race2Runs = 0;

        IScheduledJob[] BuildJobs(bool gateJob1)
        {
            return [new DelegateJob("probe.race.1", "@hourly", async ct =>
                {
                    _ = Interlocked.Increment(ref race1Runs);
                    if (gateJob1)
                    {
                        _ = job1Entered.TrySetResult();
                        await job1Release.Task.WaitAsync(ct);
                    }

                    return JobExecutionResult.Success();
                }), new DelegateJob("probe.race.2", "@hourly", ct =>
                {
                    _ = ct;
                    _ = Interlocked.Increment(ref race2Runs);
                    return Task.FromResult(JobExecutionResult.Success());
                })];
        }

        await using var nodeA = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-a",
            fakeTime,
            jobs: BuildJobs(gateJob1: true)
        );
        await using var nodeB = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-b",
            fakeTime,
            jobs: BuildJobs(gateJob1: false)
        );

        await EnsureAndSeedDueJobsAsync(nodeA, nowUtc, "probe.race.1", "probe.race.2");

        var engineA = nodeA.GetRequiredService<ScheduledJobEngine>();
        var engineB = nodeB.GetRequiredService<ScheduledJobEngine>();
        await engineA.InitializeAsync();
        await engineB.InitializeAsync();

        var taskA = Task.Run(() => engineA.RunDueJobsOnceAsync());
        await job1Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

        var nodeBCount = await engineB.RunDueJobsOnceAsync();
        _ = job1Release.TrySetResult();
        var nodeACount = await taskA;

        Assert.Equal(1, nodeACount);
        Assert.Equal(1, nodeBCount);
        Assert.Equal(1, race1Runs);
        Assert.Equal(1, race2Runs);
        await AssertSingleHistoryAndNextHourAsync(nodeA, "probe.race.2", nowUtc.AddHours(1));
    }

    [Fact]
    public async Task RunDueJobsOnceAsync_Skips_Job_Deactivated_Mid_Sweep()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var nowUtc = new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(nowUtc);
        var job1Entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var job1Release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var deact1Runs = 0;
        var deact2Runs = 0;

        await using var nodeA = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-a",
            fakeTime,
            jobs: [new DelegateJob("probe.deact.1", "@hourly", async ct =>
                {
                    _ = Interlocked.Increment(ref deact1Runs);
                    _ = job1Entered.TrySetResult();
                    await job1Release.Task.WaitAsync(ct);
                    return JobExecutionResult.Success();
                }), new DelegateJob("probe.deact.2", "@hourly", ct =>
                {
                    _ = ct;
                    _ = Interlocked.Increment(ref deact2Runs);
                    return Task.FromResult(JobExecutionResult.Success());
                })]
        );

        await EnsureAndSeedDueJobsAsync(nodeA, nowUtc, "probe.deact.1", "probe.deact.2");
        var engineA = nodeA.GetRequiredService<ScheduledJobEngine>();

        var taskA = Task.Run(() => engineA.RunDueJobsOnceAsync());
        await job1Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

        await DeactivateJobAsync(nodeA, "probe.deact.2");
        _ = job1Release.TrySetResult();
        var ranCount = await taskA;

        Assert.Equal(1, ranCount);
        Assert.Equal(1, deact1Runs);
        Assert.Equal(0, deact2Runs);
        await AssertZeroHistoriesAsync(nodeA, "probe.deact.2");
    }

    private static async Task EnsureAndSeedDueJobsAsync(
        ServiceProvider provider,
        DateTimeOffset nowUtc,
        string firstKey,
        string secondKey
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, SchedulerDbContext.SchemaName);
        db.ScheduledJobs.AddRange(
            ScheduledJob.Create(
                Guid.CreateVersion7(),
                firstKey,
                firstKey,
                "@hourly",
                nowUtc.AddSeconds(-20)
            ),
            ScheduledJob.Create(
                Guid.CreateVersion7(),
                secondKey,
                secondKey,
                "@hourly",
                nowUtc.AddSeconds(-10)
            )
        );
        _ = await db.SaveChangesAsync();
    }

    private static async Task DeactivateJobAsync(ServiceProvider provider, string jobKey)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        const bool inactive = false;
        _ = await db
            .ScheduledJobs.Where(x => x.JobKey == jobKey)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, inactive));
    }

    private static async Task AssertSingleHistoryAndNextHourAsync(
        ServiceProvider provider,
        string jobKey,
        DateTimeOffset expectedNextRunUtc
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var jobRow = await db.ScheduledJobs.AsNoTracking().SingleAsync(x => x.JobKey == jobKey);
        Assert.Equal(expectedNextRunUtc, jobRow.NextRunUtc);
        Assert.Equal(
            1,
            await db.ScheduledJobHistories.CountAsync(h => h.ScheduledJobId == jobRow.Id)
        );
    }

    private static async Task AssertZeroHistoriesAsync(ServiceProvider provider, string jobKey)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var jobRow = await db.ScheduledJobs.AsNoTracking().SingleAsync(x => x.JobKey == jobKey);
        Assert.Equal(
            0,
            await db.ScheduledJobHistories.CountAsync(h => h.ScheduledJobId == jobRow.Id)
        );
    }

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
}
