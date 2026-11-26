using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.Modules.Lms.Tests.Fixtures;

public sealed class LmsContainersFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("ruth_lms_base")
        .WithUsername("ruth")
        .WithPassword("ruth_secret")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public async Task<string> CreateIsolatedDatabaseAsync()
    {
        var dbName = $"ruth_lms_{Guid.CreateVersion7():N}";
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\";";
        _ = await cmd.ExecuteNonQueryAsync();

        var builder = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = dbName,
        };
        return builder.ConnectionString;
    }
}
