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
public sealed class ScheduledJobEngineResilienceTests(Postgres18ContainerFixture fixture)
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
    public async Task NonPositive_HistoryCount_And_DefaultHistoryCount_FallBack_To_500_And_Orphan_History_Recovers()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 5, 3, 0, 0, 0, TimeSpan.Zero));
        var counter = 0;

        var job = new DelegateJob("test.fallback500", "@hourly", ct =>
        {
            ct.ThrowIfCancellationRequested();
            var label = (++counter).ToString(CultureInfo.InvariantCulture);
            return Task.FromResult(JobExecutionResult.Success($"Run #{label}"));
        });

        await using var provider = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-fallback",
            fakeTime,
            configureOptions: opts => opts.DefaultHistoryCount = 0,
            jobs: job
        );
        await EnsureSchemaAsync(provider);

        var engine = provider.GetRequiredService<ScheduledJobEngine>();
        await engine.InitializeAsync();
        await SetZeroHistoryCountAsync(provider, "test.fallback500");

        for (var i = 0; i < 4; i++)
        {
            fakeTime.Advance(TimeSpan.FromSeconds(1));
            Assert.True(await engine.TriggerJobNowAsync("test.fallback500"));
        }

        await AssertHistoryCountAsync(provider, 4);

        var orphanHistoryId = await SeedOrphanRunningAsync(
            provider,
            "test.fallback500",
            fakeTime.GetUtcNow()
        );
        fakeTime.Advance(TimeSpan.FromSeconds(2));
        await engine.InitializeAsync();

        await AssertOrphanHistoryFailedAsync(provider, orphanHistoryId);
    }

    [Fact]
    public async Task Graceful_Cancellation_Transitions_Job_And_History_To_Failed()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero));
        using var cancelCts = new CancellationTokenSource();

        var cancelJob = new DelegateJob("test.cancel", "@hourly", async ct =>
        {
            await cancelCts.CancelAsync();
            ct.ThrowIfCancellationRequested();
            return JobExecutionResult.Success();
        });

        await using var provider = SchedulerTestHelper.BuildProvider(
            connectionString,
            "node-cancel",
            fakeTime,
            jobs: cancelJob
        );
        await EnsureSchemaAsync(provider);

        var engine = provider.GetRequiredService<ScheduledJobEngine>();
        await engine.InitializeAsync(CancellationToken.None);

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            engine.TriggerJobNowAsync("test.cancel", cancelCts.Token)
        );

        await AssertCanceledJobFailedAsync(provider, "test.cancel");
    }

    private static async Task EnsureSchemaAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, SchedulerDbContext.SchemaName);
    }

    private static async Task SetZeroHistoryCountAsync(ServiceProvider provider, string jobKey)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        _ = await db
            .ScheduledJobs.Where(x => x.JobKey == jobKey)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.HistoryCount, 0));
    }

    private static async Task AssertHistoryCountAsync(ServiceProvider provider, int expected)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        Assert.Equal(expected, await db.ScheduledJobHistories.CountAsync());
    }

    private static async Task<Guid> SeedOrphanRunningAsync(
        ServiceProvider provider,
        string jobKey,
        DateTimeOffset startedUtc
    )
    {
        var orphanHistoryId = Guid.CreateVersion7();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var row = await db.ScheduledJobs.SingleAsync(x => x.JobKey == jobKey);
        row.MarkRunning(startedUtc, "dead-node");
        _ = db.ScheduledJobHistories.Add(
            ScheduledJobHistory.Start(orphanHistoryId, row.Id, "dead-node", startedUtc)
        );
        _ = await db.SaveChangesAsync();
        return orphanHistoryId;
    }

    private static async Task AssertOrphanHistoryFailedAsync(
        ServiceProvider provider,
        Guid orphanHistoryId
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var orphanHistory = await db.ScheduledJobHistories.SingleAsync(x =>
            x.Id == orphanHistoryId
        );
        Assert.Equal(ScheduledJobStatus.Failed, orphanHistory.Status);
        _ = Assert.NotNull(orphanHistory.CompletedOnUtc);
    }

    private static async Task AssertCanceledJobFailedAsync(ServiceProvider provider, string jobKey)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var canceledJob = await db.ScheduledJobs.SingleAsync(x => x.JobKey == jobKey);
        Assert.Equal(ScheduledJobStatus.Failed, canceledJob.LastStatus);
        var canceledHistory = await db
            .ScheduledJobHistories.Where(h => h.ScheduledJobId == canceledJob.Id)
            .SingleAsync();
        Assert.Equal(ScheduledJobStatus.Failed, canceledHistory.Status);
        _ = Assert.NotNull(canceledHistory.CompletedOnUtc);
    }
}
