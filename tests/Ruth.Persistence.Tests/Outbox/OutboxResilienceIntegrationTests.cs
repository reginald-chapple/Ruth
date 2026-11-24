using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;
using Xunit;

namespace Ruth.Persistence.Tests.Outbox;

[Collection("Postgres18")]
public sealed class OutboxResilienceIntegrationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class WorkerGate(bool failAfterRelease = true)
    {
        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool FailAfterRelease { get; } = failAfterRelease;
    }

    private sealed class GatedTestConsumer(WorkerGate gate)
        : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
    {
        public async Task ConsumeAsync(
            TestWidgetIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            _ = gate.Entered.TrySetResult();
            await gate.Release.Task;
            if (gate.FailAfterRelease)
            {
                throw new TimeoutException("Slow worker timed out after lease expired.");
            }
        }
    }

    [Fact]
    public async Task Slow_Failing_Worker_After_Lease_Expiry_Cannot_Overwrite_Processed_Outbox_Row()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero));
        var gate = new WorkerGate(failAfterRelease: true);

        await using var slowProvider = BuildGatedProvider(connectionString, fakeTime, gate);
        await using var fastProvider = OutboxTestHelper.BuildProvider(
            connectionString,
            new OutboxTestConsumerState(),
            fakeTime
        );

        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "LeaseOverlap");
        await SeedSingleEventAsync(slowProvider, evt);

        var slowDispatcher = slowProvider.GetRequiredService<OutboxDispatcherService>();
        var fastDispatcher = fastProvider.GetRequiredService<OutboxDispatcherService>();

        var slowTask = slowDispatcher.ProcessPendingOnceAsync();
        await gate.Entered.Task;

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(1, await fastDispatcher.ProcessPendingOnceAsync());

        _ = gate.Release.TrySetResult();
        _ = await slowTask;

        var row = await ReadOutboxRowAsync(fastProvider, evt.EventId);
        _ = Assert.NotNull(row.ProcessedOnUtc);
        Assert.Null(row.Error);
        Assert.Null(row.NextRetryOnUtc);
        Assert.Null(row.LockedUntilUtc);
        Assert.Null(row.LockToken);
        Assert.Equal(0, row.RetryCount);
    }

    [Fact]
    public async Task Late_DeadLetter_After_Lease_Expiry_Does_Not_Overwrite_InFlight_Reclaim_And_Allows_Completion()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 1, 0, 0, TimeSpan.Zero));
        var gateA = new WorkerGate(failAfterRelease: true);
        var gateB = new WorkerGate(failAfterRelease: false);

        await using var providerA = BuildGatedProvider(connectionString, fakeTime, gateA, opts =>
            opts.MaxRetries = 1
        );
        await using var providerB = BuildGatedProvider(connectionString, fakeTime, gateB, opts =>
            opts.MaxRetries = 1
        );

        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "FenceDeadLetter3Bii");
        await SeedSingleEventAsync(providerA, evt);

        var dispatcherA = providerA.GetRequiredService<OutboxDispatcherService>();
        var dispatcherB = providerB.GetRequiredService<OutboxDispatcherService>();

        var taskA = dispatcherA.ProcessPendingOnceAsync();
        await gateA.Entered.Task;

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        var taskB = dispatcherB.ProcessPendingOnceAsync();
        await gateB.Entered.Task;

        var inFlightRow = await ReadOutboxRowAsync(providerB, evt.EventId);
        _ = Assert.NotNull(inFlightRow.LockToken);
        _ = Assert.NotNull(inFlightRow.LockedUntilUtc);

        _ = gateA.Release.TrySetResult();
        Assert.Equal(1, await taskA);

        var afterLateFailRow = await ReadOutboxRowAsync(providerB, evt.EventId);
        Assert.Null(afterLateFailRow.DeadLetteredOnUtc);
        Assert.Null(afterLateFailRow.ProcessedOnUtc);
        Assert.Null(afterLateFailRow.Error);
        Assert.Equal(0, afterLateFailRow.RetryCount);
        Assert.Equal(inFlightRow.LockToken, afterLateFailRow.LockToken);
        Assert.Equal(inFlightRow.LockedUntilUtc, afterLateFailRow.LockedUntilUtc);

        _ = gateB.Release.TrySetResult();
        Assert.Equal(1, await taskB);

        var finalRow = await ReadOutboxRowAsync(providerB, evt.EventId);
        _ = Assert.NotNull(finalRow.ProcessedOnUtc);
        Assert.Null(finalRow.DeadLetteredOnUtc);
        Assert.Null(finalRow.LockedUntilUtc);
        Assert.Null(finalRow.LockToken);
        Assert.Null(finalRow.Error);
    }

    [Fact]
    public async Task Late_RetryFailure_After_Lease_Expiry_Does_Not_Clear_Active_Lease_Or_Allow_Third_Worker_Claim()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 4, 2, 0, 0, TimeSpan.Zero));
        var gateA = new WorkerGate(failAfterRelease: true);
        var gateB = new WorkerGate(failAfterRelease: false);

        static void ConfigureZeroBackoff(OutboxOptions opts)
        {
            opts.MaxRetries = 3;
            opts.BaseRetryDelay = TimeSpan.Zero;
            opts.MaxRetryDelay = TimeSpan.Zero;
        }

        await using var providerA = BuildGatedProvider(
            connectionString,
            fakeTime,
            gateA,
            ConfigureZeroBackoff
        );
        await using var providerB = BuildGatedProvider(
            connectionString,
            fakeTime,
            gateB,
            ConfigureZeroBackoff
        );
        await using var providerC = OutboxTestHelper.BuildProvider(
            connectionString,
            new OutboxTestConsumerState(),
            fakeTime,
            configureOutbox: opts =>
            {
                opts.LockDuration = TimeSpan.FromSeconds(1);
                ConfigureZeroBackoff(opts);
            }
        );

        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "FenceRetry3Biii");
        await SeedSingleEventAsync(providerA, evt);

        var taskA = providerA
            .GetRequiredService<OutboxDispatcherService>()
            .ProcessPendingOnceAsync();
        await gateA.Entered.Task;

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        var taskB = providerB
            .GetRequiredService<OutboxDispatcherService>()
            .ProcessPendingOnceAsync();
        await gateB.Entered.Task;

        var inFlightRow = await ReadOutboxRowAsync(providerB, evt.EventId);
        _ = Assert.NotNull(inFlightRow.LockToken);
        _ = Assert.NotNull(inFlightRow.LockedUntilUtc);

        _ = gateA.Release.TrySetResult();
        Assert.Equal(1, await taskA);

        var afterLateFailRow = await ReadOutboxRowAsync(providerB, evt.EventId);
        Assert.Equal(inFlightRow.LockToken, afterLateFailRow.LockToken);
        Assert.Equal(inFlightRow.LockedUntilUtc, afterLateFailRow.LockedUntilUtc);
        Assert.Equal(0, afterLateFailRow.RetryCount);
        Assert.Null(afterLateFailRow.NextRetryOnUtc);
        Assert.Null(afterLateFailRow.Error);

        Assert.Equal(
            0,
            await providerC.GetRequiredService<OutboxDispatcherService>().ProcessPendingOnceAsync()
        );

        _ = gateB.Release.TrySetResult();
        Assert.Equal(1, await taskB);

        var finalRow = await ReadOutboxRowAsync(providerB, evt.EventId);
        _ = Assert.NotNull(finalRow.ProcessedOnUtc);
        Assert.Null(finalRow.LockedUntilUtc);
        Assert.Null(finalRow.LockToken);
        Assert.Null(finalRow.Error);
        Assert.Equal(0, finalRow.RetryCount);
    }

    private static ServiceProvider BuildGatedProvider(
        string connectionString,
        FakeTimeProvider fakeTime,
        WorkerGate gate,
        Action<OutboxOptions>? configureOutbox = null
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSharedKernel(fakeTime);
        _ = services.AddSingleton(gate);
        _ = services.AddRuthPersistence(connectionString, opts =>
        {
            opts.LockDuration = TimeSpan.FromSeconds(1);
            opts.MaxRetries = 3;
            configureOutbox?.Invoke(opts);
        });
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            GatedTestConsumer,
            AlphaModuleDbContext
        >();
        return services.BuildServiceProvider();
    }

    private static async Task SeedSingleEventAsync(
        ServiceProvider provider,
        TestWidgetIntegrationEvent evt
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
        db.EnqueueIntegrationEvent(evt);
        _ = await db.SaveChangesAsync();
    }

    private static async Task<OutboxMessage> ReadOutboxRowAsync(
        ServiceProvider provider,
        Guid eventId
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        return await db.OutboxMessages.AsNoTracking().SingleAsync(x => x.Id == eventId);
    }
}
