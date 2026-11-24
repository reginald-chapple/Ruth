using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Events;
using Xunit;

namespace Ruth.Persistence.Tests.EntityFramework;

[Collection("Postgres18")]
public sealed class AuditAndConcurrencyInterceptorTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class RecordingDualConsumer(List<TestWidgetDualEvent> received)
        : IDomainEventConsumer<TestWidgetDualEvent>
    {
        public async Task ConsumeAsync(
            TestWidgetDualEvent domainEvent,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Delay(5, cancellationToken);
            received.Add(domainEvent);
        }
    }

    private sealed class SingleThreadCheckingSyncContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            throw new InvalidOperationException(
                "Continuation attempted to post back to blocked SynchronizationContext."
            );
        }
    }

    [Fact]
    public async Task DualEvent_ConcurrencyRetry_On_Same_DbContext_Succeeds_With_Single_Outbox_And_Domain_Dispatch()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var dispatched = new List<TestWidgetDualEvent>();
        await using var provider = BuildProvider(connectionString, dispatched);
        var widgetId = await SeedWidgetAsync(provider);

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();

        var w1 = await db1.AuditedWidgets.SingleAsync(x => x.Id == widgetId);
        var w2 = await db2.AuditedWidgets.SingleAsync(x => x.Id == widgetId);

        w1.Rename("Winner");
        _ = await db1.SaveChangesAsync();

        w2.RenameWithDualEvent("RetriedWinner");
        var conflict = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            db2.SaveChangesAsync()
        );
        var entry = Assert.Single(conflict.Entries);
        var dbValues = await entry.GetDatabaseValuesAsync();
        Assert.NotNull(dbValues);
        entry.OriginalValues.SetValues(dbValues);

        _ = await db2.SaveChangesAsync();

        Assert.Empty(w2.DomainEvents);
        var delivered = Assert.Single(dispatched);
        Assert.Equal("RetriedWinner", delivered.Name);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        Assert.Equal(1, await verifyDb.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Failed_Save_Detach_And_Save_Unrelated_On_Same_DbContext_Writes_Zero_Phantom_Outbox_Messages()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var dispatched = new List<TestWidgetDualEvent>();
        await using var provider = BuildProvider(connectionString, dispatched);

        var w1Id = await SeedWidgetAsync(provider, ensureSchema: true);
        var w2Id = await SeedWidgetAsync(provider, ensureSchema: false);

        await AssertDetachedFailureLeaksZeroOutboxAsync(
            provider,
            w1Id,
            useDualEvent: true,
            dispatched
        );
        await AssertDetachedFailureLeaksZeroOutboxAsync(
            provider,
            w2Id,
            useDualEvent: false,
            dispatched
        );
    }

    [Fact]
    public async Task Sync_SaveChanges_Under_NonNull_SynchronizationContext_Does_Not_Deadlock()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var dispatched = new List<TestWidgetDualEvent>();
        await using var provider = BuildProvider(connectionString, dispatched);
        var widgetId = await SeedWidgetAsync(provider);

        AssertSyncSaveChangesUnderSyncContext(provider, widgetId);

        var delivered = Assert.Single(dispatched);
        Assert.Equal("SyncContextSafe", delivered.Name);
    }

    private static void AssertSyncSaveChangesUnderSyncContext(
        ServiceProvider provider,
        TestWidgetId widgetId
    )
    {
        var previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new SingleThreadCheckingSyncContext());
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            var widget = db.AuditedWidgets.Single(x => x.Id == widgetId);
            widget.RenameWithDualEvent("SyncContextSafe");
            _ = db.SaveChanges();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static async Task AssertDetachedFailureLeaksZeroOutboxAsync(
        ServiceProvider provider,
        TestWidgetId targetId,
        bool useDualEvent,
        List<TestWidgetDualEvent> dispatched
    )
    {
        await using var winScope = provider.CreateAsyncScope();
        var winDb = winScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var winner = await winDb.AuditedWidgets.SingleAsync(x => x.Id == targetId);

        await using var failScope = provider.CreateAsyncScope();
        var failDb = failScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var loser = await failDb.AuditedWidgets.SingleAsync(x => x.Id == targetId);

        winner.Rename("CommittedElsewhere");
        _ = await winDb.SaveChangesAsync();

        if (useDualEvent)
        {
            loser.RenameWithDualEvent("AbandonedDual");
        }
        else
        {
            loser.Rename("AbandonedIntegration", emitIntegrationEvent: true);
        }

        _ = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => failDb.SaveChangesAsync());
        if (!useDualEvent)
        {
            _ = Assert.Single(loser.IntegrationEvents);
        }

        failDb.Entry(loser).State = EntityState.Detached;
        _ = failDb.AuditedWidgets.Add(
            new AuditedWidget(TestWidgetId.Create(), "Unrelated", DateTimeOffset.UtcNow)
        );
        _ = await failDb.SaveChangesAsync();

        Assert.Empty(dispatched);
        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        Assert.Equal(0, await verifyDb.OutboxMessages.CountAsync());
    }

    private static ServiceProvider BuildProvider(
        string connectionString,
        List<TestWidgetDualEvent> dispatched
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSharedKernel(new FakeTimeProvider(DateTimeOffset.UtcNow));
        _ = services.AddSingleton(dispatched);
        _ = services.AddDomainEventConsumer<TestWidgetDualEvent, RecordingDualConsumer>();
        _ = services.AddRuthPersistence(connectionString);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        return services.BuildServiceProvider();
    }

    private static async Task<TestWidgetId> SeedWidgetAsync(
        ServiceProvider provider,
        bool ensureSchema = true
    )
    {
        var id = TestWidgetId.Create();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        if (ensureSchema)
        {
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
        }

        _ = db.AuditedWidgets.Add(new AuditedWidget(id, "Initial", DateTimeOffset.UtcNow));
        _ = await db.SaveChangesAsync();
        return id;
    }
}
