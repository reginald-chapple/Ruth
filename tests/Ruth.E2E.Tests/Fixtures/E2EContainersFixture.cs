using System.Globalization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.E2E.Tests.Fixtures;

public sealed class E2EContainersFixture : IAsyncLifetime, IAsyncDisposable
{
    private const string PostgresUser = "ruth";
    private const string PostgresPassword = "ruth_secret";
    private const string DefaultDatabase = "ruth";
    private const string PostgresE2EImageTag = "ruth-postgres18-e2e:latest";

    private PostgreSqlContainer? _postgresContainer;
    private IContainer? _redisContainer;
    private IContainer? _rustFsContainer;

    public string PostgresAdminConnectionString { get; private set; } = string.Empty;

    public string RedisConnectionString { get; private set; } = string.Empty;

    public string RustFsEndpoint { get; private set; } = string.Empty;

    public string RustFsConsoleEndpoint { get; private set; } = string.Empty;

    public const string RustFsAccessKey = "rustfsadmin";

    public const string RustFsSecretKey = "rustfsadmin";

    public async Task InitializeAsync()
    {
        var postgresImage = await BuildPostgresImageAsync();

        _postgresContainer = new PostgreSqlBuilder(postgresImage)
            .WithDatabase(DefaultDatabase)
            .WithUsername(PostgresUser)
            .WithPassword(PostgresPassword)
            .Build();

        _redisContainer = new ContainerBuilder("redis:8-alpine")
            .WithPortBinding(6379, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("redis-cli", "ping"))
            .Build();

        _rustFsContainer = new ContainerBuilder("rustfs/rustfs:latest")
            .WithEnvironment("RUSTFS_ACCESS_KEY", RustFsAccessKey)
            .WithEnvironment("RUSTFS_SECRET_KEY", RustFsSecretKey)
            .WithPortBinding(9000, assignRandomHostPort: true)
            .WithPortBinding(9001, assignRandomHostPort: true)
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

        PostgresAdminConnectionString = _postgresContainer.GetConnectionString();
        RedisConnectionString =
            $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}";
        RustFsEndpoint =
            $"http://{_rustFsContainer.Hostname}:{_rustFsContainer.GetMappedPublicPort(9000)}";
        RustFsConsoleEndpoint =
            $"http://{_rustFsContainer.Hostname}:{_rustFsContainer.GetMappedPublicPort(9001)}";

        await PostgresDatabaseInitializer.EnsureExtensionsAsync(
            PostgresAdminConnectionString,
            CancellationToken.None
        );
    }

    public string GetConnectionString(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        PostgresDatabaseInitializer.ValidateIdentifier(databaseName);

        var builder = new NpgsqlConnectionStringBuilder(PostgresAdminConnectionString)
        {
            Database = databaseName,
        };

        return builder.ConnectionString;
    }

    public async Task<string> CreateIsolatedDatabaseAsync(CancellationToken ct = default)
    {
        var postgres = EnsurePostgresStarted();
        var databaseName = $"ruth_e2e_{Guid.CreateVersion7():N}";

        await ExecutePsqlAsync(postgres, $"CREATE DATABASE \"{databaseName}\";", ct);

        var isolatedConnectionString = GetConnectionString(databaseName);
        await PostgresDatabaseInitializer.EnsureExtensionsAsync(isolatedConnectionString, ct);
        return databaseName;
    }

    public async Task DropDatabaseAsync(string databaseName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        var resolvedName = PostgresDatabaseInitializer.ExtractDatabaseName(databaseName);
        PostgresDatabaseInitializer.ValidateIdentifier(resolvedName);

        var postgres = EnsurePostgresStarted();
        var targetConnectionString = GetConnectionString(resolvedName);

        await using (var targetConnection = new NpgsqlConnection(targetConnectionString))
        {
            NpgsqlConnection.ClearPool(targetConnection);
        }

        await PostgresDatabaseInitializer.TerminateDatabaseConnectionsAsync(
            PostgresAdminConnectionString,
            resolvedName,
            ct
        );

        await ExecutePsqlAsync(postgres, $"DROP DATABASE IF EXISTS \"{resolvedName}\";", ct);
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

    private static async Task<IFutureDockerImage> BuildPostgresImageAsync()
    {
        var postgresContextDirectory = Path.Combine(
            PostgresDatabaseInitializer.FindRepositoryRoot(),
            "deploy",
            "docker",
            "postgres"
        );

        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(postgresContextDirectory)
            .WithDockerfile("Dockerfile")
            .WithName(PostgresE2EImageTag)
            .WithImageBuildPolicy(PullPolicy.Missing)
            .WithDeleteIfExists(false)
            .WithCleanUp(false)
            .Build();

        await image.CreateAsync();
        return image;
    }

    private PostgreSqlContainer EnsurePostgresStarted()
    {
        return _postgresContainer
            ?? throw new InvalidOperationException("E2EContainersFixture is not initialized.");
    }

    private static async Task ExecutePsqlAsync(
        PostgreSqlContainer postgres,
        string sql,
        CancellationToken ct
    )
    {
        var result = await postgres.ExecAsync(
            ["psql", "-U", PostgresUser, "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-c", sql],
            ct
        );

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "PostgreSQL command failed ({0}): {1}",
                    result.ExitCode,
                    result.Stderr
                )
            );
        }
    }
}
