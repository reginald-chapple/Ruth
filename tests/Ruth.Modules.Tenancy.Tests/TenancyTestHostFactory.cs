using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.Modules.Tenancy.Tests;

internal static class TenancyTestHostFactory
{
    private sealed class RecordingSeedContributor(List<TenantId> seededTenants)
        : ITenantSeedContributor
    {
        public int Order => 25;

        public Task SeedTenantAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default
        )
        {
            Assert.Equal(tenantId, AmbientTenantContext.CurrentTenantId);
            seededTenants.Add(tenantId);
            return Task.CompletedTask;
        }
    }

    public static async Task<ServiceProvider> BuildMigratedProviderAsync(
        PostgreSqlContainer postgres,
        List<TenantId> seededTenants
    )
    {
        var dbName = $"tenancy_{Guid.NewGuid():N}";
        await using (var adminConn = new NpgsqlConnection(postgres.GetConnectionString()))
        {
            await adminConn.OpenAsync();
            await using var createDbCmd = adminConn.CreateCommand();
            createDbCmd.CommandText = $"CREATE DATABASE \"{dbName}\";";
            _ = await createDbCmd.ExecuteNonQueryAsync();
        }

        var connString = new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
        {
            Database = dbName,
        }.ConnectionString;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:RuthDatabase"] = connString,
                    ["Bootstrap:AutoMigrateAndSeed"] = "true",
                    ["Tenancy:SandboxBaseDomain"] = "sandbox.localhost",
                    ["Tenancy:PlatformOperatorHost"] = "ops.localhost",
                    ["Tenancy:SeedOperatorEmail"] = "compliance@ops.localhost",
                    ["Tenancy:SeedOperatorPassword"] = "OperatorPass!2026#",
                }
            )
            .Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(config);
        _ = services.AddSingleton<ITenantSeedContributor>(
            new RecordingSeedContributor(seededTenants)
        );
        _ = services.AddRuthPersistence(config);
        _ = services.AddTenancyModule(config);

        var provider = services.BuildServiceProvider();
        foreach (var hosted in provider.GetServices<IHostedService>())
        {
            if (hosted.GetType().Name.Contains("Bootstrap", StringComparison.Ordinal))
            {
                await hosted.StartAsync(CancellationToken.None);
            }
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await provisioner.EnsureAppRoleAndSchemaGrantsAsync("tenant_probe");
        return provider;
    }

    public static async Task AssertAppRoleCannotReadPlatformSchemaAsync(ServiceProvider provider)
    {
        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        var appConnStr = provisioner.BuildTenantRuntimeConnectionString(
            provisioner.AdminConnectionString
        );
        await using var appConn = new NpgsqlConnection(appConnStr);
        await appConn.OpenAsync();
        await using var cmd = appConn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM platform.tenants;";
        var ex = await Assert.ThrowsAsync<PostgresException>(cmd.ExecuteScalarAsync);
        Assert.Equal("42501", ex.SqlState);
    }
}
