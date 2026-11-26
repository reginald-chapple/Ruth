using Npgsql;
using Ruth.SharedKernel.Tenancy;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.Persistence.Tests.Fixtures;

public sealed class Postgres18ContainerFixture : IAsyncLifetime
{
    public static readonly TenantId DefaultTenantId = TenantId.From(
        new Guid(0x01950000, 0x0000, 0x7000, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01)
    );

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("ruth_admin")
        .WithUsername("ruth")
        .WithPassword("ruth_test_pw")
        .Build();

    public string AdminConnectionString => _container.GetConnectionString();

    public Task InitializeAsync()
    {
        AmbientTenantContext.SetTestHarnessDefault(DefaultTenantId);
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    public async Task<string> CreateIsolatedDatabaseAsync()
    {
        var dbName = $"ruth_test_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{dbName}\";";
        _ = await command.ExecuteNonQueryAsync();

        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Database = dbName,
        };
        return builder.ConnectionString;
    }
}
