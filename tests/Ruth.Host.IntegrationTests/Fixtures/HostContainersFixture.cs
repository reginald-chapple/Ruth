using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Ruth.Host.IntegrationTests.Fixtures;

public sealed class HostContainersFixture : IAsyncLifetime, IAsyncDisposable
{
    private const string PostgresPostGisImageTag = "ruth-postgres18-e2e:latest";

    private PostgreSqlContainer? _postgresContainer;
    private RedisContainer? _redisContainer;
    private IContainer? _rustFsContainer;

    public string PostgresConnectionString { get; private set; } = string.Empty;

    public string RedisConnectionString { get; private set; } = string.Empty;

    public string RustFsEndpoint { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var postgresImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(
                CommonDirectoryPath.GetSolutionDirectory(),
                "deploy/docker/postgres"
            )
            .WithDockerfile("Dockerfile")
            .WithName(PostgresPostGisImageTag)
            .WithDeleteIfExists(false)
            .Build();
        await postgresImage.CreateAsync();

        _postgresContainer = new PostgreSqlBuilder(postgresImage)
            .WithDatabase("ruth")
            .WithUsername("ruth")
            .WithPassword("ruth_secret")
            .Build();

        _redisContainer = new RedisBuilder("redis:8-alpine").Build();

        _rustFsContainer = new ContainerBuilder("rustfs/rustfs:latest")
            .WithEnvironment("RUSTFS_ACCESS_KEY", "rustfsadmin")
            .WithEnvironment("RUSTFS_SECRET_KEY", "rustfsadmin")
            .WithPortBinding(9000, assignRandomHostPort: true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(9000).ForPath("/health"))
            )
            .Build();

        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rustFsContainer.StartAsync()
        );

        PostgresConnectionString = _postgresContainer.GetConnectionString();
        RedisConnectionString = _redisContainer.GetConnectionString();
        RustFsEndpoint =
            $"http://{_rustFsContainer.Hostname}:{_rustFsContainer.GetMappedPublicPort(9000)}";
    }

    public async Task<string> CreateIsolatedDatabaseAsync(
        CancellationToken cancellationToken = default
    )
    {
        var dbName = $"ruth_host_test_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{dbName}\";";
        _ = await command.ExecuteNonQueryAsync(cancellationToken);

        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionString)
        {
            Database = dbName,
        };
        return builder.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_rustFsContainer is not null)
        {
            await _rustFsContainer.DisposeAsync();
        }

        if (_redisContainer is not null)
        {
            await _redisContainer.DisposeAsync();
        }

        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }
}
