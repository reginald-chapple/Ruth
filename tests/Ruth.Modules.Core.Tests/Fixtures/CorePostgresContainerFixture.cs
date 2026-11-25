using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.Modules.Core.Tests.Fixtures;

public sealed class CorePostgresContainerFixture : IAsyncLifetime
{
    private const string PostgresPostGisImageTag = "ruth-postgres18-e2e:latest";

    private PostgreSqlContainer? _container;
    private IContainer? _rustFsContainer;

    public string AdminConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container is not initialized.");

    public string RustFsEndpoint { get; private set; } = string.Empty;

    public const string RustFsAccessKey = "rustfsadmin";

    public const string RustFsSecretKey = "rustfsadmin";

    public async Task InitializeAsync()
    {
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(
                CommonDirectoryPath.GetSolutionDirectory(),
                "deploy/docker/postgres"
            )
            .WithDockerfile("Dockerfile")
            .WithName(PostgresPostGisImageTag)
            .WithDeleteIfExists(false)
            .Build();
        await image.CreateAsync();

        _container = new PostgreSqlBuilder(image)
            .WithDatabase("ruth_core_admin")
            .WithUsername("ruth")
            .WithPassword("ruth_test_pw")
            .Build();

        _rustFsContainer = new ContainerBuilder("rustfs/rustfs:latest")
            .WithEnvironment("RUSTFS_ACCESS_KEY", RustFsAccessKey)
            .WithEnvironment("RUSTFS_SECRET_KEY", RustFsSecretKey)
            .WithPortBinding(9000, assignRandomHostPort: true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(9000).ForPath("/health"))
            )
            .Build();

        await Task.WhenAll(_container.StartAsync(), _rustFsContainer.StartAsync());
        RustFsEndpoint =
            $"http://{_rustFsContainer.Hostname}:{_rustFsContainer.GetMappedPublicPort(9000)}";
    }

    public async Task DisposeAsync()
    {
        if (_rustFsContainer is not null)
        {
            await _rustFsContainer.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public async Task<string> CreateIsolatedDatabaseAsync()
    {
        var dbName = $"ruth_core_test_{Guid.NewGuid():N}";
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
