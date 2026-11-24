using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Events;
using Xunit;

namespace Ruth.Persistence.Tests.EntityFramework;

[Collection("Postgres18")]
public sealed class ModuleDbContextIntegrationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class RecordingDomainConsumer(List<TestWidgetDomainEvent> received)
        : IDomainEventConsumer<TestWidgetDomainEvent>
    {
        public Task ConsumeAsync(
            TestWidgetDomainEvent domainEvent,
            CancellationToken cancellationToken = default
        )
        {
            received.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ModuleDbContexts_Isolate_Schemas_Convert_StronglyTypedIds_And_Normalize_Utc()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        await using var provider = BuildModuleProvider(connectionString);

        await SeedAlphaAndBetaAsync(provider);
        var tables = await ReadTableNamesAsync(connectionString);

        Assert.Contains("alpha.outbox_messages", tables, StringComparer.Ordinal);
        Assert.Contains("alpha.inbox_messages", tables, StringComparer.Ordinal);
        Assert.Contains("alpha.audited_widgets", tables, StringComparer.Ordinal);
        Assert.Contains("beta.outbox_messages", tables, StringComparer.Ordinal);
        Assert.Contains("beta.inbox_messages", tables, StringComparer.Ordinal);
        Assert.Contains("beta.beta_widgets", tables, StringComparer.Ordinal);

        await using var readScope = provider.CreateAsyncScope();
        var readAlpha = readScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var loaded = await readAlpha.AuditedWidgets.AsNoTracking().SingleAsync();
        Assert.Equal(TimeSpan.Zero, loaded.EffectiveAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 17, 30, 0, TimeSpan.Zero), loaded.EffectiveAt);
    }

    [Fact]
    public async Task Interceptor_Stamps_Audit_Timestamps_And_Detects_Concurrency_Conflicts_And_Discards_Failed_Domain_Events()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var initialUtc = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(initialUtc);
        var dispatchedEvents = new List<TestWidgetDomainEvent>();

        await using var provider = BuildModuleProvider(
            connectionString,
            fakeTime,
            dispatchedEvents
        );
        var widgetId = await SeedAuditedWidgetAsync(provider, initialUtc);

        await AssertConcurrentUpdateConflictAsync(
            provider,
            widgetId,
            initialUtc,
            fakeTime,
            dispatchedEvents
        );
    }

    [Fact]
    public async Task Postgres_Xmin_RowVersion_Detects_Concurrent_Updates()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        await using var provider = BuildModuleProvider(connectionString);
        var widgetId = TestWidgetId.Create();

        await using (var setupScope = provider.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
            _ = db.RowVersionedWidgets.Add(new RowVersionedWidget(widgetId, "Initial"));
            _ = await db.SaveChangesAsync();
        }

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();

        var entity1 = await db1.RowVersionedWidgets.SingleAsync(x => x.Id == widgetId);
        var entity2 = await db2.RowVersionedWidgets.SingleAsync(x => x.Id == widgetId);

        entity1.ChangeLabel("UpdatedFirst");
        _ = await db1.SaveChangesAsync();

        entity2.ChangeLabel("UpdatedSecond");
        _ = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
    }

    [Fact]
    public async Task Sync_SaveChanges_Dispatches_DomainEvents_And_Failed_Save_Discards_Staged_Events()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var initialUtc = new DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(initialUtc);
        var dispatchedEvents = new List<TestWidgetDomainEvent>();

        await using var provider = BuildModuleProvider(
            connectionString,
            fakeTime,
            dispatchedEvents
        );
        var widgetId = await SeedAuditedWidgetAsync(provider, initialUtc);
        AssertSyncSaveChangesBehavior(provider, widgetId, initialUtc, dispatchedEvents);
    }

    private static void AssertSyncSaveChangesBehavior(
        ServiceProvider provider,
        TestWidgetId widgetId,
        DateTimeOffset initialUtc,
        List<TestWidgetDomainEvent> dispatchedEvents
    )
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var widget = db.AuditedWidgets.Single(x => x.Id == widgetId);
        widget.Rename("SyncWinner");
        _ = db.SaveChanges();

        _ = Assert.Single(dispatchedEvents);
        Assert.Empty(widget.DomainEvents);

        var duplicate = new AuditedWidget(widgetId, "Dup", initialUtc);
        duplicate.Rename("FailedSyncEvent");
        using var failScope = provider.CreateScope();
        var failDb = failScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        _ = failDb.AuditedWidgets.Add(duplicate);
        _ = Assert.Throws<DbUpdateException>(() => failDb.SaveChanges());
        _ = Assert.Single(dispatchedEvents);
    }

    private static ServiceProvider BuildModuleProvider(
        string connectionString,
        FakeTimeProvider? fakeTime = null,
        List<TestWidgetDomainEvent>? dispatchedEvents = null
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        if (fakeTime is not null)
        {
            _ = services.AddSharedKernel(fakeTime);
        }

        if (dispatchedEvents is not null)
        {
            _ = services.AddSingleton(dispatchedEvents);
            _ = services.AddDomainEventConsumer<TestWidgetDomainEvent, RecordingDomainConsumer>();
        }

        _ = services.AddRuthPersistence(connectionString);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        _ = services.AddModuleDbContext<BetaModuleDbContext>(connectionString, "beta");
        return services.BuildServiceProvider();
    }

    private static async Task SeedAlphaAndBetaAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var alpha = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var beta = scope.ServiceProvider.GetRequiredService<BetaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(alpha, "alpha");
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(beta, "beta");

        var nonUtc = new DateTimeOffset(2026, 6, 1, 12, 30, 0, TimeSpan.FromHours(-5));
        _ = alpha.AuditedWidgets.Add(new AuditedWidget(TestWidgetId.Create(), "AlphaItem", nonUtc));
        _ = beta.BetaWidgets.Add(new RowVersionedWidget(TestWidgetId.Create(), "BetaItem"));

        _ = await alpha.SaveChangesAsync();
        _ = await beta.SaveChangesAsync();
    }

    private static async Task<List<string>> ReadTableNamesAsync(string connectionString)
    {
        await using var verifyConnection = new NpgsqlConnection(connectionString);
        await verifyConnection.OpenAsync();
        await using var cmd = verifyConnection.CreateCommand();
        cmd.CommandText =
            "SELECT table_schema, table_name FROM information_schema.tables WHERE table_schema IN ('alpha', 'beta') ORDER BY table_schema, table_name;";

        var tables = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        return tables;
    }

    private static async Task<TestWidgetId> SeedAuditedWidgetAsync(
        ServiceProvider provider,
        DateTimeOffset initialUtc
    )
    {
        var widgetId = TestWidgetId.Create();
        await using var setupScope = provider.CreateAsyncScope();
        var db = setupScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");

        var widget = new AuditedWidget(widgetId, "Original", initialUtc);
        _ = db.AuditedWidgets.Add(widget);
        _ = await db.SaveChangesAsync();

        Assert.Equal(initialUtc, widget.CreatedOnUtc);
        Assert.Null(widget.ModifiedOnUtc);
        Assert.NotEqual(Guid.Empty, widget.ConcurrencyStamp);
        return widgetId;
    }

    private static async Task AssertConcurrentUpdateConflictAsync(
        ServiceProvider provider,
        TestWidgetId widgetId,
        DateTimeOffset initialUtc,
        FakeTimeProvider fakeTime,
        List<TestWidgetDomainEvent> dispatchedEvents
    )
    {
        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();

        var w1 = await db1.AuditedWidgets.SingleAsync(x => x.Id == widgetId);
        var w2 = await db2.AuditedWidgets.SingleAsync(x => x.Id == widgetId);

        fakeTime.Advance(TimeSpan.FromMinutes(15));
        var t1 = fakeTime.GetUtcNow();
        w1.Rename("Winner", emitIntegrationEvent: true);
        _ = await db1.SaveChangesAsync();

        Assert.Equal(initialUtc, w1.CreatedOnUtc);
        Assert.Equal(t1, w1.ModifiedOnUtc);
        Assert.Empty(w1.DomainEvents);
        _ = Assert.Single(dispatchedEvents);
        Assert.Equal("Winner", dispatchedEvents[0].Name);
        Assert.Equal(1, await db1.OutboxMessages.CountAsync());

        fakeTime.Advance(TimeSpan.FromMinutes(5));
        w2.Rename("Loser", emitIntegrationEvent: false);
        _ = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());

        _ = Assert.Single(w2.DomainEvents);
        _ = Assert.Single(dispatchedEvents);
    }
}
