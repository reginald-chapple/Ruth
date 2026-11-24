using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.Scheduling;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.SharedKernel.Jobs;
using Xunit;

namespace Ruth.Persistence.Tests.Scheduling;

[Collection("Postgres18")]
public sealed class AdvisoryLockIntegrationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class GatedSlowJob(
        string jobKey,
        TaskCompletionSource started,
        TaskCompletionSource release,
        Action onExecuted
    ) : IScheduledJob
    {
        public string JobKey { get; } = jobKey;
        public string DefaultCronExpression => "*/5 * * * *";

        public async Task<JobExecutionResult> ExecuteAsync(
            CancellationToken cancellationToken = default
        )
        {
            onExecuted();
            _ = started.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return JobExecutionResult.Success("Slow job done.");
        }
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

    [Fact]
    public async Task Cross_Node_Advisory_Lock_Allows_Only_One_Concurrent_Execution()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executionCount = 0;

        await using var node1 = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-1",
            jobs: new GatedSlowJob("test.slow", started, release, () =>
                _ = Interlocked.Increment(ref executionCount)
            )
        );
        await using var node2 = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-2",
            jobs: new GatedSlowJob("test.slow", started, release, () =>
                _ = Interlocked.Increment(ref executionCount)
            )
        );

        await using (var scope = node1.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(
                db,
                SchedulerDbContext.SchemaName
            );
        }

        var engine1 = node1.GetRequiredService<ScheduledJobEngine>();
        var engine2 = node2.GetRequiredService<ScheduledJobEngine>();
        await engine1.InitializeAsync();
        await engine2.InitializeAsync();

        var task1 = Task.Run(() => engine1.TriggerJobNowAsync("test.slow"));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var task2Result = await engine2.TriggerJobNowAsync("test.slow");
        release.SetResult();
        var task1Result = await task1;

        Assert.True(task1Result);
        Assert.False(task2Result);
        Assert.Equal(1, executionCount);

        await using var verifyScope = node1.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var histories = await verifyDb.ScheduledJobHistories.ToListAsync();
        var history = Assert.Single(histories);
        Assert.Equal(ScheduledJobStatus.Success, history.Status);
        Assert.Equal("node-1", history.WorkerId);
    }

    [Fact]
    public async Task Reentrancy_Guard_Rejects_Same_Flow_Reacquisition_And_Permits_Sequential()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        await using var provider = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-reentrant"
        );

        var lockProvider = provider.GetRequiredService<PostgresAdvisoryLockProvider>();

        await using (var handle = await lockProvider.TryAcquireAsync("reentrant.key"))
        {
            Assert.NotNull(handle);
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                lockProvider.TryAcquireAsync("reentrant.key")
            );
        }

        await using var secondHandle = await lockProvider.TryAcquireAsync("reentrant.key");
        Assert.NotNull(secondHandle);
    }

    [Fact]
    public async Task Recursive_Trigger_Inside_Running_Job_Fails_Cleanly_Without_Deadlock()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        ScheduledJobEngine? engine = null;

        var recursiveJob = new DelegateJob("test.recursive", "@daily", async ct =>
        {
            _ = await engine!.TriggerJobNowAsync("test.recursive", ct);
            return JobExecutionResult.Success();
        });

        await using var provider = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-rec",
            jobs: recursiveJob
        );
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(
                db,
                SchedulerDbContext.SchemaName
            );
        }

        engine = provider.GetRequiredService<ScheduledJobEngine>();
        await engine.InitializeAsync();

        var ran = await engine.TriggerJobNowAsync("test.recursive");
        Assert.True(ran);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var jobRow = await verifyDb.ScheduledJobs.SingleAsync(x => x.JobKey == "test.recursive");
        Assert.Equal(ScheduledJobStatus.Failed, jobRow.LastStatus);
        Assert.Contains(
            "InvalidOperationException",
            jobRow.LastStatusMessage,
            StringComparison.Ordinal
        );
    }
}
