using Npgsql;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Fixtures;

public sealed class CmsContainersFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("ruth_cms_base")
        .WithUsername("ruth")
        .WithPassword("ruth_secret")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine").Build();

    public string RedisConnectionString => _redis.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    public async Task<string> CreateIsolatedDatabaseAsync()
    {
        var dbName = $"ruth_cms_{Guid.CreateVersion7():N}";
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

    public Task<ConnectionMultiplexer> ConnectRedisAsync()
    {
        return ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
    }
}
