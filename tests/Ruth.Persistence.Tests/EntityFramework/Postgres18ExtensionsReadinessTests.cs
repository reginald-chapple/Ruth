using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Tests.Fixtures;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.Persistence.Tests.EntityFramework;

public sealed class Postgres18ExtensionsReadinessTests
{
    [Fact]
    public async Task Postgres18_Image_Supports_Postgis_And_Pgvector_With_ModuleDbContext_Hooks()
    {
        var repoRoot = CommonDirectoryPath.GetSolutionDirectory().DirectoryPath;
        var initScriptPath = Path.Combine(
            repoRoot,
            "deploy",
            "init-scripts",
            "01-init-extensions.sql"
        );

        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(
                CommonDirectoryPath.GetSolutionDirectory(),
                "deploy/docker/postgres"
            )
            .WithDockerfile("Dockerfile")
            .WithName("ruth-postgres18-postgis-test:latest")
            .WithDeleteIfExists(false)
            .Build();

        await image.CreateAsync();

        await using var container = new PostgreSqlBuilder(image)
            .WithDatabase("ruth_ext_test")
            .WithUsername("ruth")
            .WithPassword("ruth_test_pw")
            .WithResourceMapping(new FileInfo(initScriptPath), "/docker-entrypoint-initdb.d/")
            .Build();

        await container.StartAsync();

        var connectionString = container.GetConnectionString();
        await using var provider = BuildProvider(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");

        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync();

        var extensions = await LoadInstalledExtensionsAsync(connection);
        Assert.Contains("uuid-ossp", extensions, StringComparer.Ordinal);
        Assert.Contains("postgis", extensions, StringComparer.Ordinal);
        Assert.Contains("btree_gist", extensions, StringComparer.Ordinal);
        Assert.Contains("vector", extensions, StringComparer.Ordinal);

        await AssertExtensionsQueryableAsync(connection);
    }

    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRuthPersistence(connectionString);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        return services.BuildServiceProvider();
    }

    private static async Task<HashSet<string>> LoadInstalledExtensionsAsync(
        NpgsqlConnection connection
    )
    {
        await using var extCmd = connection.CreateCommand();
        extCmd.CommandText = "SELECT extname FROM pg_extension ORDER BY extname;";
        var extensions = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await extCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            _ = extensions.Add(reader.GetString(0));
        }

        return extensions;
    }

    private static async Task AssertExtensionsQueryableAsync(NpgsqlConnection connection)
    {
        await using var probeCmd = connection.CreateCommand();
        probeCmd.CommandText = "SELECT PostGIS_Version(), ('[1,2,3]'::vector)::text;";
        await using var probeReader = await probeCmd.ExecuteReaderAsync();
        Assert.True(await probeReader.ReadAsync());
        Assert.False(string.IsNullOrWhiteSpace(probeReader.GetString(0)));
        Assert.Equal("[1,2,3]", probeReader.GetString(1));
    }
}
