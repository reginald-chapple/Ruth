using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.EntityFramework;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Persistence.Tests.EntityFramework;

[Collection("Postgres18")]
public sealed class MultiTenantRlsHardeningIntegrationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    [Fact]
    public async Task Dual_Roles_Migrator_NonSuperuser_And_AppRole_ControlPlane_And_MigrationsHistory_Lockdown_Work()
    {
        var adminConnStr = await _fixture.CreateIsolatedDatabaseAsync();
        var provisioner = new PostgresRlsRoleProvisioner(
            adminConnStr,
            Options.Create(new RlsOptions())
        );

        await provisioner.EnsureAppRoleAndSchemaGrantsAsync("alpha");

        var migratorConnStr = provisioner.BuildMigratorConnectionString(adminConnStr);
        var migratorProvisioner = new PostgresRlsRoleProvisioner(
            migratorConnStr,
            Options.Create(new RlsOptions())
        );
        await migratorProvisioner.EnsureAppRoleAndSchemaGrantsAsync("alpha_mig");

        await using (var migConn = new NpgsqlConnection(migratorConnStr))
        {
            await migConn.OpenAsync();
            await using var cmd = migConn.CreateCommand();
            cmd.CommandText = string.Join(
                '\n',
                "CREATE SCHEMA IF NOT EXISTS scheduler;",
                "CREATE TABLE IF NOT EXISTS scheduler.jobs_probe (id uuid PRIMARY KEY);",
                "CREATE TABLE IF NOT EXISTS alpha_mig.\"__EFMigrationsHistory\" (\"MigrationId\" text PRIMARY KEY);",
                "SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = current_user;"
            );
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.False(reader.GetBoolean(0));
            Assert.False(reader.GetBoolean(1));
        }

        await migratorProvisioner.EnsureAppRoleAndSchemaGrantsAsync("alpha_mig");
        await provisioner.LockDownControlPlaneSchemaAsync("scheduler");

        var appConnStr = provisioner.BuildTenantRuntimeConnectionString(adminConnStr);
        await using var appConn = new NpgsqlConnection(appConnStr);
        await appConn.OpenAsync();

        await AssertSqlStateDeniedAsync(appConn, "SELECT * FROM scheduler.jobs_probe;");
        await AssertSqlStateDeniedAsync(
            appConn,
            "SELECT * FROM alpha_mig.\"__EFMigrationsHistory\";"
        );
    }

    [Fact]
    public async Task WriteGuard_And_ConnectionSynchronizer_Block_PropertyMutation_PoolReset_And_Rollback_Guc_Leak()
    {
        var connStr = await _fixture.CreateIsolatedDatabaseAsync();
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRuthPersistence(connStr);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connStr, "alpha");
        await using var provider = services.BuildServiceProvider();

        using (AmbientTenantContext.BeginScope(tenantA))
        {
            await using var scopeA = provider.CreateAsyncScope();
            var dbA = scopeA.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(dbA, "alpha");
            var widgetA = new AuditedWidget(
                TestWidgetId.Create(),
                "SecretA",
                DateTimeOffset.UtcNow
            );
            _ = dbA.AuditedWidgets.Add(widgetA);
            _ = await dbA.SaveChangesAsync();

            using (AmbientTenantContext.BeginScope(tenantB))
            {
                dbA.Entry(widgetA).Property(nameof(ITenantScoped.TenantId)).CurrentValue = tenantB;
                dbA.Entry(widgetA).State = EntityState.Modified;
                _ = await Assert.ThrowsAsync<CrossTenantAccessException>(() =>
                    dbA.SaveChangesAsync()
                );
            }
        }

        await VerifyPoolResetAndRollbackIsolationAsync(provider, tenantA, tenantB);
    }

    private static async Task VerifyPoolResetAndRollbackIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        TenantId tenantB
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();

        using (AmbientTenantContext.BeginScope(tenantA))
        {
            await db.Database.OpenConnectionAsync();
            var conn = db.Database.GetDbConnection();

            await conn.CloseAsync();
            await conn.OpenAsync();
            _ = Assert.Single(await db.AuditedWidgets.AsNoTracking().ToListAsync());

            await using (await db.Database.BeginTransactionAsync())
            {
                using (AmbientTenantContext.BeginScope(tenantB))
                {
                    Assert.Empty(
                        await db.AuditedWidgets.IgnoreQueryFilters().AsNoTracking().ToListAsync()
                    );
                }
            }

            using (AmbientTenantContext.BeginScope(tenantB))
            {
                var afterRollback = await db
                    .AuditedWidgets.IgnoreQueryFilters()
                    .AsNoTracking()
                    .ToListAsync();
                Assert.Empty(afterRollback);
            }
        }
    }

    private static async Task AssertSqlStateDeniedAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var ex = await Assert.ThrowsAsync<PostgresException>(cmd.ExecuteReaderAsync);
        Assert.Equal("42501", ex.SqlState);
    }
}
