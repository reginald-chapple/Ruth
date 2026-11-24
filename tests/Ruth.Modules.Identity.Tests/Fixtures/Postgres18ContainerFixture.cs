using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Fixtures;

public sealed class Postgres18ContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("ruth_identity_admin")
        .WithUsername("ruth")
        .WithPassword("ruth_test_pw")
        .Build();

    public string AdminConnectionString => _container.GetConnectionString();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    public async Task<string> CreateIsolatedDatabaseAsync()
    {
        var dbName = $"ruth_identity_test_{Guid.NewGuid():N}";
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
