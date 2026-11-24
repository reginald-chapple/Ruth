using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Persistence.Tests.Outbox;

[Collection("Postgres18")]
public sealed class OutboxRetentionCleanupJobTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class RecordingProcessor(string schema, int deletedReturn)
        : IOutboxModuleProcessor
    {
        public string Schema { get; } = schema;
        public DateTimeOffset? LastCutoffUtc { get; private set; }
        public int CallCount { get; private set; }

        public Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task<int> PruneProcessedAsync(
            DateTimeOffset cutoffUtc,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
            LastCutoffUtc = cutoffUtc;
            return Task.FromResult(deletedReturn);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ComputesCutoffFromClockAndRetentionPeriod()
    {
        var nowUtc = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var clock = new SystemClock(new FakeTimeProvider(nowUtc));
        var alphaProcessor = new RecordingProcessor("alpha", 3);
        var betaProcessor = new RecordingProcessor("beta", 2);
        var options = Options.Create(
            new OutboxOptions { ProcessedRetentionPeriod = TimeSpan.FromDays(7) }
        );

        var job = new OutboxRetentionCleanupJob([alphaProcessor, betaProcessor], options, clock);
        var result = await job.ExecuteAsync();

        var expectedCutoff = nowUtc - TimeSpan.FromDays(7);
        Assert.Equal(JobExecutionStatus.Success, result.Status);
        Assert.Equal(expectedCutoff, alphaProcessor.LastCutoffUtc);
        Assert.Equal(expectedCutoff, betaProcessor.LastCutoffUtc);
        Assert.Contains(
            "Pruned 5 processed Outbox message(s)",
            result.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithNegativeRetentionPeriod_ReturnsWarningAndSkipsProcessors()
    {
        var nowUtc = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var clock = new SystemClock(new FakeTimeProvider(nowUtc));
        var processor = new RecordingProcessor("alpha", 5);
        var options = Options.Create(
            new OutboxOptions { ProcessedRetentionPeriod = TimeSpan.FromSeconds(-1) }
        );

        var job = new OutboxRetentionCleanupJob([processor], options, clock);
        var result = await job.ExecuteAsync();

        Assert.Equal(JobExecutionStatus.Warning, result.Status);
        Assert.Equal(0, processor.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesOnlyProcessedMessagesOlderThanCutoffAcrossSchemas()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var nowUtc = new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

        await using var provider = await BuildRetentionProviderAsync(connectionString, nowUtc);
        var ids = await SeedOutboxRowsAsync(provider, nowUtc);

        var cleanupJob = provider
            .GetServices<IScheduledJob>()
            .OfType<OutboxRetentionCleanupJob>()
            .Single();
        var result = await cleanupJob.ExecuteAsync();

        Assert.Equal(JobExecutionStatus.Success, result.Status);
        Assert.Contains(
            "Pruned 2 processed Outbox message(s)",
            result.Message,
            StringComparison.Ordinal
        );

        await AssertRemainingRowsAsync(provider, ids);
    }

    private static async Task<ServiceProvider> BuildRetentionProviderAsync(
        string connectionString,
        DateTimeOffset nowUtc
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSharedKernel(new FakeTimeProvider(nowUtc));
        _ = services.AddRuthPersistence(connectionString, configureOutbox: opts =>
            opts.ProcessedRetentionPeriod = TimeSpan.FromDays(7)
        );
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        _ = services.AddModuleDbContext<BetaModuleDbContext>(connectionString, "beta");

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var alpha = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var beta = scope.ServiceProvider.GetRequiredService<BetaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(alpha, "alpha");
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(beta, "beta");
        return provider;
    }

    private static async Task<(
        Guid OldProcessed,
        Guid RecentProcessed,
        Guid PendingOld,
        Guid DeadLetterOld,
        Guid BetaOldProcessed
    )> SeedOutboxRowsAsync(ServiceProvider provider, DateTimeOffset nowUtc)
    {
        var oldProcessedId = Guid.CreateVersion7();
        var recentProcessedId = Guid.CreateVersion7();
        var pendingOldId = Guid.CreateVersion7();
        var deadLetterOldId = Guid.CreateVersion7();
        var betaOldProcessedId = Guid.CreateVersion7();

        await using var scope = provider.CreateAsyncScope();
        var alpha = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var beta = scope.ServiceProvider.GetRequiredService<BetaModuleDbContext>();

        var oldTime = nowUtc.AddDays(-10);
        var recentTime = nowUtc.AddDays(-2);

        alpha.OutboxMessages.AddRange(
            OutboxMessage.Create(oldProcessedId, "Test.Event", "{}", oldTime),
            OutboxMessage.Create(recentProcessedId, "Test.Event", "{}", recentTime),
            OutboxMessage.Create(pendingOldId, "Test.Event", "{}", oldTime),
            OutboxMessage.Create(deadLetterOldId, "Test.Event", "{}", oldTime)
        );
        _ = beta.OutboxMessages.Add(
            OutboxMessage.Create(betaOldProcessedId, "Test.Event", "{}", oldTime)
        );
        _ = await alpha.SaveChangesAsync();
        _ = await beta.SaveChangesAsync();

        _ = await alpha
            .OutboxMessages.Where(x => x.Id == oldProcessedId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedOnUtc, oldTime));
        _ = await alpha
            .OutboxMessages.Where(x => x.Id == recentProcessedId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedOnUtc, recentTime));
        _ = await alpha
            .OutboxMessages.Where(x => x.Id == deadLetterOldId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DeadLetteredOnUtc, oldTime));
        _ = await beta
            .OutboxMessages.Where(x => x.Id == betaOldProcessedId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedOnUtc, oldTime));

        return (
            oldProcessedId,
            recentProcessedId,
            pendingOldId,
            deadLetterOldId,
            betaOldProcessedId
        );
    }

    private static async Task AssertRemainingRowsAsync(
        ServiceProvider provider,
        (
            Guid OldProcessed,
            Guid RecentProcessed,
            Guid PendingOld,
            Guid DeadLetterOld,
            Guid BetaOldProcessed
        ) ids
    )
    {
        await using var verifyScope = provider.CreateAsyncScope();
        var alpha = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var beta = verifyScope.ServiceProvider.GetRequiredService<BetaModuleDbContext>();

        var remainingAlpha = await alpha
            .OutboxMessages.AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync();
        Assert.DoesNotContain(ids.OldProcessed, remainingAlpha);
        Assert.Contains(ids.RecentProcessed, remainingAlpha);
        Assert.Contains(ids.PendingOld, remainingAlpha);
        Assert.Contains(ids.DeadLetterOld, remainingAlpha);

        var remainingBeta = await beta
            .OutboxMessages.AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync();
        Assert.DoesNotContain(ids.BetaOldProcessed, remainingBeta);
    }
}
