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
public sealed class IdempotentMultiConsumerTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    [Fact]
    public async Task Overlapping_Lease_Duplicate_Attempt_Executes_Each_Consumer_Once()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(
            new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
        );
        var counters = new ConsumerExecutionCounters();
        var gate1 = new MultiConsumerGate(pauseConsumerA: true);
        var gate2 = new MultiConsumerGate();

        await using var provider1 = BuildProvider(connectionString, fakeTime, gate1, counters);
        await using var provider2 = BuildProvider(connectionString, fakeTime, gate2, counters);

        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "Probe4B");
        await SeedEventAsync(provider1, evt);

        var task1 = provider1
            .GetRequiredService<OutboxDispatcherService>()
            .ProcessPendingOnceAsync();
        await gate1.EnteredA.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

        // Expire the Outbox lease while provider1 is still inside ConsumerA;
        // provider2 re-claims the Outbox row and waits on the PostgreSQL Inbox claim lock.
        fakeTime.Advance(TimeSpan.FromSeconds(5));
        var task2 = provider2
            .GetRequiredService<OutboxDispatcherService>()
            .ProcessPendingOnceAsync();

        await Task.Delay(TimeSpan.FromMilliseconds(150), TimeProvider.System);
        Assert.Equal(1, counters.ConsumerACount);

        _ = gate1.ReleaseA.TrySetResult();
        _ = await Task.WhenAll(task1, task2);

        Assert.Equal(1, counters.ConsumerACount);
        Assert.Equal(1, counters.ConsumerBCount);
        await AssertProbe4BResultsAsync(provider2, evt.EventId);
    }

    [Fact]
    public async Task Failed_Consumer_RollsBack_InboxClaim_And_EntityChanges_Allowing_Clean_Retry()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(
            new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero)
        );
        var existingId = TestWidgetId.Create();
        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "ModifyRollback");
        var failureState = new FlakyFailureState(failFirstAttempt: true);

        await using var provider = BuildFlakyProvider(
            connectionString,
            fakeTime,
            existingId,
            failureState
        );
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
            _ = db.AuditedWidgets.Add(
                new AuditedWidget(existingId, "Original", fakeTime.GetUtcNow())
            );
            db.EnqueueIntegrationEvent(evt);
            _ = await db.SaveChangesAsync();
        }

        // First attempt throws inside ConsumerA after mutating widget; both mutation and inbox claim roll back.
        Assert.Equal(
            1,
            await provider.GetRequiredService<OutboxDispatcherService>().ProcessPendingOnceAsync()
        );

        await using (var midScope = provider.CreateAsyncScope())
        {
            var midDb = midScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            var midWidget = await midDb
                .AuditedWidgets.AsNoTracking()
                .SingleAsync(x => x.Id == existingId);
            Assert.Equal("Original", midWidget.Name);
            Assert.Equal(0, await midDb.InboxMessages.CountAsync(x => x.MessageId == evt.EventId));
        }

        // Advance past retry backoff; second attempt succeeds cleanly.
        fakeTime.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(
            1,
            await provider.GetRequiredService<OutboxDispatcherService>().ProcessPendingOnceAsync()
        );

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var widget = await verifyDb
            .AuditedWidgets.AsNoTracking()
            .SingleAsync(x => x.Id == existingId);
        Assert.Equal("CommittedOnRetry", widget.Name);
        Assert.Equal(
            1,
            await verifyDb.AuditedWidgets.CountAsync(x => x.Name == $"ConsumerB-{evt.EventId}")
        );
        Assert.Equal(2, await verifyDb.InboxMessages.CountAsync(x => x.MessageId == evt.EventId));
    }

    private static ServiceProvider BuildProvider(
        string connectionString,
        FakeTimeProvider fakeTime,
        MultiConsumerGate gate,
        ConsumerExecutionCounters counters
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSharedKernel(fakeTime);
        _ = services.AddSingleton(gate);
        _ = services.AddSingleton(counters);
        _ = services.AddRuthPersistence(connectionString, opts =>
            opts.LockDuration = TimeSpan.FromSeconds(1)
        );
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            GatedConsumerA,
            AlphaModuleDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            GatedConsumerB,
            AlphaModuleDbContext
        >();
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildFlakyProvider(
        string connectionString,
        FakeTimeProvider fakeTime,
        TestWidgetId existingId,
        FlakyFailureState failureState
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSharedKernel(fakeTime);
        _ = services.AddSingleton(new ModifyRaceTarget(existingId));
        _ = services.AddSingleton(failureState);
        _ = services.AddSingleton(new ConsumerExecutionCounters());
        _ = services.AddRuthPersistence(connectionString);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            FlakyModifyingConsumerA,
            AlphaModuleDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            GatedConsumerB,
            AlphaModuleDbContext
        >();
        _ = services.AddSingleton(new MultiConsumerGate());
        return services.BuildServiceProvider();
    }

    private static async Task SeedEventAsync(
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

    private static async Task AssertProbe4BResultsAsync(ServiceProvider provider, Guid eventId)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        Assert.Equal(1, await db.AuditedWidgets.CountAsync(x => x.Name == $"ConsumerA-{eventId}"));
        Assert.Equal(1, await db.AuditedWidgets.CountAsync(x => x.Name == $"ConsumerB-{eventId}"));
        Assert.Equal(2, await db.InboxMessages.CountAsync(x => x.MessageId == eventId));

        var outbox = await db.OutboxMessages.AsNoTracking().SingleAsync(x => x.Id == eventId);
        _ = Assert.NotNull(outbox.ProcessedOnUtc);
        Assert.Null(outbox.LockToken);
    }

    private sealed class MultiConsumerGate(bool pauseConsumerA = false, bool pauseConsumerB = false)
    {
        public bool PauseConsumerA { get; } = pauseConsumerA;
        public bool PauseConsumerB { get; } = pauseConsumerB;
        public TaskCompletionSource EnteredA { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseA { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource EnteredB { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseB { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ConsumerExecutionCounters
    {
        private int _consumerA;
        private int _consumerB;

        public int ConsumerACount => Volatile.Read(ref _consumerA);
        public int ConsumerBCount => Volatile.Read(ref _consumerB);

        public void IncrementA()
        {
            _ = Interlocked.Increment(ref _consumerA);
        }

        public void IncrementB()
        {
            _ = Interlocked.Increment(ref _consumerB);
        }
    }

    private sealed class FlakyFailureState(bool failFirstAttempt)
    {
        private int _shouldFail = failFirstAttempt ? 1 : 0;

        public bool ConsumeFailure()
        {
            return Interlocked.Exchange(ref _shouldFail, 0) == 1;
        }
    }

    private sealed record ModifyRaceTarget(TestWidgetId WidgetId);

    private sealed class GatedConsumerA(
        AlphaModuleDbContext db,
        MultiConsumerGate gate,
        ConsumerExecutionCounters counters
    ) : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
    {
        public async Task ConsumeAsync(
            TestWidgetIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            counters.IncrementA();
            _ = db.AuditedWidgets.Add(
                new AuditedWidget(
                    TestWidgetId.Create(),
                    $"ConsumerA-{integrationEvent.EventId}",
                    DateTimeOffset.UtcNow
                )
            );
            if (gate.PauseConsumerA)
            {
                _ = gate.EnteredA.TrySetResult();
                await gate.ReleaseA.Task.WaitAsync(cancellationToken);
            }
        }
    }

    private sealed class GatedConsumerB(
        AlphaModuleDbContext db,
        MultiConsumerGate gate,
        ConsumerExecutionCounters counters
    ) : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
    {
        public async Task ConsumeAsync(
            TestWidgetIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            counters.IncrementB();
            _ = db.AuditedWidgets.Add(
                new AuditedWidget(
                    TestWidgetId.Create(),
                    $"ConsumerB-{integrationEvent.EventId}",
                    DateTimeOffset.UtcNow
                )
            );
            if (gate.PauseConsumerB)
            {
                _ = gate.EnteredB.TrySetResult();
                await gate.ReleaseB.Task.WaitAsync(cancellationToken);
            }
        }
    }

    private sealed class FlakyModifyingConsumerA(
        AlphaModuleDbContext db,
        ModifyRaceTarget target,
        FlakyFailureState failureState
    ) : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
    {
        public async Task ConsumeAsync(
            TestWidgetIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            var widget = await db.AuditedWidgets.SingleAsync(
                x => x.Id == target.WidgetId,
                cancellationToken
            );

            if (failureState.ConsumeFailure())
            {
                widget.Rename("RejectedMutation");
                throw new InvalidOperationException("Simulated transient consumer failure.");
            }

            widget.Rename("CommittedOnRetry");
        }
    }
}
