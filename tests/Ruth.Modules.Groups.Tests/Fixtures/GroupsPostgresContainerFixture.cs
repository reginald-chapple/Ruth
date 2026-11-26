using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Fixtures;

public sealed class GroupsPostgresContainerFixture : IAsyncLifetime
{
    private const string PostgresImageTag = "ruth-postgres18-e2e:latest";

    private PostgreSqlContainer? _container;

    public string AdminConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container is not initialized.");

    public async Task InitializeAsync()
    {
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(
                CommonDirectoryPath.GetSolutionDirectory(),
                "deploy/docker/postgres"
            )
            .WithDockerfile("Dockerfile")
            .WithName(PostgresImageTag)
            .WithImageBuildPolicy(PullPolicy.Missing)
            .WithDeleteIfExists(false)
            .WithCleanUp(false)
            .Build();
        await image.CreateAsync();

        _container = new PostgreSqlBuilder(image)
            .WithDatabase("ruth_groups_admin")
            .WithUsername("ruth")
            .WithPassword("ruth_test_pw")
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public async Task<string> CreateIsolatedDatabaseAsync()
    {
        var dbName = $"ruth_groups_test_{Guid.NewGuid():N}";
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
