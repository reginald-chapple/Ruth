using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Scheduling;
using Ruth.Persistence.Tests.Fixtures;
using Xunit;

namespace Ruth.Persistence.Tests.Scheduling;

[Collection("Postgres18")]
public sealed class SchedulerBootstrapHostedServiceTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    [Fact]
    public async Task StartAsync_WhenAutoMigrateDisabled_DoesNotCreateSchedulerSchema()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        await using var provider = BuildProvider(connectionString, autoMigrate: false);

        var bootstrap = provider
            .GetServices<IHostedService>()
            .OfType<SchedulerBootstrapHostedService>()
            .Single();
        await bootstrap.StartAsync(CancellationToken.None);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = 'scheduler';";
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        Assert.Equal(0L, count);
    }

    [Fact]
    public async Task StartAsync_WhenAutoMigrateEnabled_AppliesMigrationsAndEngineSyncsInitialDueJob()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        await using var provider = BuildProvider(connectionString, autoMigrate: true);

        var bootstrap = provider
            .GetServices<IHostedService>()
            .OfType<SchedulerBootstrapHostedService>()
            .Single();
        await bootstrap.StartAsync(CancellationToken.None);

        var engine = provider.GetRequiredService<ScheduledJobEngine>();
        await engine.InitializeAsync();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        _ = Assert.Single(appliedMigrations);

        var jobRow = await db
            .ScheduledJobs.AsNoTracking()
            .SingleAsync(x => x.JobKey == "ruth.persistence.outbox-retention-cleanup");
        Assert.Equal("0 * * * *", jobRow.CronExpression);
        Assert.Equal(500, jobRow.HistoryCount);
        _ = Assert.NotNull(jobRow.NextRunUtc);
        Assert.True(jobRow.NextRunUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ScheduledJobEngine_WhenDatabaseUnreachable_DoesNotCrashOnStartAndStopsCleanly()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var deadPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var deadConnection = string.Create(
            CultureInfo.InvariantCulture,
            $"Host=127.0.0.1;Port={deadPort};Database=ruth;Username=ruth;Password=ruth_secret;Timeout=1"
        );
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRuthPersistence(deadConnection, configureScheduler: opts =>
            opts.PollingInterval = TimeSpan.FromMilliseconds(50)
        );

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<ScheduledJobEngine>();

        await engine.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(150));
        await engine.StopAsync(CancellationToken.None);
    }

    private static ServiceProvider BuildProvider(string connectionString, bool autoMigrate)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:RuthDatabase"] = connectionString,
                    ["Bootstrap:AutoMigrateAndSeed"] = autoMigrate ? "true" : "false",
                }
            )
            .Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddRuthPersistence(configuration);
        return services.BuildServiceProvider();
    }
}
