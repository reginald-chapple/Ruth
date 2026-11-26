using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Persistence.Tests.EntityFramework;

[Collection("Postgres18")]
public sealed class MultiTenantRlsIsolationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class StubActiveTenantRegistry(params TenantId[] tenants) : IActiveTenantRegistry
    {
        public ValueTask<IReadOnlyList<TenantId>> GetActiveTenantIdsAsync(
            CancellationToken cancellationToken = default
        )
        {
            return ValueTask.FromResult<IReadOnlyList<TenantId>>(tenants);
        }
    }

    private sealed class RecordingTenantConsumer(
        List<(TenantId ObservedTenantId, Guid EventId)> observed,
        ITenantContext tenantContext
    ) : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
    {
        public Task ConsumeAsync(
            TestWidgetIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            observed.Add((tenantContext.TenantId, integrationEvent.EventId));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Runtime_DbContext_Connects_As_NonSuperuser_NoBypassRls_And_Enforces_ForceRls_Even_When_IgnoreQueryFilters_Or_RawSql()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRuthPersistence(connectionString);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        await using var provider = services.BuildServiceProvider();

        await using (var initScope = provider.CreateAsyncScope())
        {
            var initDb = initScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(initDb, "alpha");
        }

        using (AmbientTenantContext.BeginScope(tenantA, "charity-a"))
        {
            await using var scopeA = provider.CreateAsyncScope();
            var dbA = scopeA.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await AssertNonSuperuserAndForceRlsMetadataAsync(dbA);

            _ = dbA.AuditedWidgets.Add(
                new AuditedWidget(TestWidgetId.Create(), "TenantA-Secret", DateTimeOffset.UtcNow)
            );
            _ = await dbA.SaveChangesAsync();
        }

        using (AmbientTenantContext.BeginScope(tenantB, "charity-b"))
        {
            await using var scopeB = provider.CreateAsyncScope();
            var dbB = scopeB.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            _ = dbB.AuditedWidgets.Add(
                new AuditedWidget(TestWidgetId.Create(), "TenantB-Secret", DateTimeOffset.UtcNow)
            );
            _ = await dbB.SaveChangesAsync();

            var visibleToB = await dbB
                .AuditedWidgets.IgnoreQueryFilters()
                .AsNoTracking()
                .ToListAsync();
            var onlyB = Assert.Single(visibleToB);
            Assert.Equal("TenantB-Secret", onlyB.Name);
            Assert.Equal(tenantB, onlyB.TenantId);

            await AssertRawSqlCrossTenantWriteBlockedAsync(dbB, tenantA);
        }

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            await using var unresolvedScope = provider.CreateAsyncScope();
            var unresolvedDb =
                unresolvedScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();

            var leaked = await unresolvedDb
                .AuditedWidgets.IgnoreQueryFilters()
                .AsNoTracking()
                .ToListAsync();
            Assert.Empty(leaked);

            _ = unresolvedDb.AuditedWidgets.Add(
                new AuditedWidget(TestWidgetId.Create(), "Unresolved", DateTimeOffset.UtcNow)
            );
            _ = await Assert.ThrowsAsync<TenantContextRequiredException>(() =>
                unresolvedDb.SaveChangesAsync()
            );
        }
    }

    [Fact]
    public async Task Outbox_Processor_Iterates_Active_Tenants_And_Preserves_TenantContext_Inside_Consumers()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();
        var observed = new List<(TenantId ObservedTenantId, Guid EventId)>();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton(observed);
        _ = services.AddSingleton<IActiveTenantRegistry>(
            new StubActiveTenantRegistry(tenantA, tenantB)
        );
        _ = services.AddRuthPersistence(connectionString);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            RecordingTenantConsumer,
            AlphaModuleDbContext
        >();
        await using var provider = services.BuildServiceProvider();

        var eventA = new TestWidgetIntegrationEvent(Guid.CreateVersion7(), "AlphaAction");
        var eventB = new TestWidgetIntegrationEvent(Guid.CreateVersion7(), "BetaAction");

        using (AmbientTenantContext.BeginScope(tenantA))
        {
            await using var scopeA = provider.CreateAsyncScope();
            var dbA = scopeA.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(dbA, "alpha");
            dbA.EnqueueIntegrationEvent(eventA);
            _ = await dbA.SaveChangesAsync();
        }

        using (AmbientTenantContext.BeginScope(tenantB))
        {
            await using var scopeB = provider.CreateAsyncScope();
            var dbB = scopeB.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            dbB.EnqueueIntegrationEvent(eventB);
            _ = await dbB.SaveChangesAsync();
        }

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            var dispatcher = provider.GetRequiredService<OutboxDispatcherService>();
            var processed = await dispatcher.ProcessPendingOnceAsync();
            Assert.Equal(2, processed);
        }

        Assert.Contains((tenantA, eventA.EventId), observed);
        Assert.Contains((tenantB, eventB.EventId), observed);
    }

    private static async Task AssertNonSuperuserAndForceRlsMetadataAsync(AlphaModuleDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();

        await using (var roleCmd = conn.CreateCommand())
        {
            roleCmd.CommandText =
                "SELECT r.rolname, r.rolsuper, r.rolbypassrls FROM pg_roles r WHERE r.rolname = current_user;";
            await using var reader = await roleCmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("ruth_app", reader.GetString(0));
            Assert.False(reader.GetBoolean(1));
            Assert.False(reader.GetBoolean(2));
        }

        await using (var rlsCmd = conn.CreateCommand())
        {
            rlsCmd.CommandText = string.Join(
                '\n',
                "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity",
                "FROM pg_class c",
                "JOIN pg_namespace n ON n.oid = c.relnamespace",
                "WHERE n.nspname = 'alpha' AND c.relkind = 'r'",
                "ORDER BY c.relname;"
            );
            var checkedTables = new List<string>();
            await using var reader = await rlsCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var tableName = reader.GetString(0);
                checkedTables.Add(tableName);
                Assert.True(reader.GetBoolean(1), $"Expected relrowsecurity on alpha.{tableName}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Expected relforcerowsecurity on alpha.{tableName}"
                );
            }

            Assert.Contains("audited_widgets", checkedTables, StringComparer.Ordinal);
            Assert.Contains("row_versioned_widgets", checkedTables, StringComparer.Ordinal);
            Assert.Contains("outbox_messages", checkedTables, StringComparer.Ordinal);
            Assert.Contains("inbox_messages", checkedTables, StringComparer.Ordinal);
        }
    }

    private static async Task AssertRawSqlCrossTenantWriteBlockedAsync(
        AlphaModuleDbContext db,
        TenantId foreignTenantId
    )
    {
        await db.Database.OpenConnectionAsync();
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = string.Join(
            '\n',
            "INSERT INTO alpha.row_versioned_widgets (\"Id\", tenant_id, \"Label\")",
            "VALUES (@id, @tid, 'SpoofedCrossTenantInsert');"
        );
        _ = cmd.Parameters.AddWithValue("id", Guid.CreateVersion7());
        _ = cmd.Parameters.AddWithValue("tid", foreignTenantId.Value);

        var ex = await Assert.ThrowsAsync<PostgresException>(cmd.ExecuteNonQueryAsync);
        Assert.Equal("42501", ex.SqlState);
    }
}
