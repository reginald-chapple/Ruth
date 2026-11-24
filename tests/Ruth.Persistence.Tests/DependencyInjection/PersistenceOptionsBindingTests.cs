using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Scheduling;
using Ruth.SharedKernel.Jobs;
using Xunit;

namespace Ruth.Persistence.Tests.DependencyInjection;

public sealed class PersistenceOptionsBindingTests
{
    private const string DummyConnectionString =
        "Host=127.0.0.1;Port=5432;Database=ruth;Username=ruth;Password=ruth_secret";

    [Fact]
    public void AddRuthPersistence_WithConfiguration_BindsOutboxAndSchedulerOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:RuthDatabase"] = DummyConnectionString,
                    ["Outbox:PollingInterval"] = "00:00:00.250",
                    ["Outbox:BatchSize"] = "12",
                    ["Outbox:LockDuration"] = "00:01:00",
                    ["Outbox:MaxRetries"] = "9",
                    ["Outbox:BaseRetryDelay"] = "00:00:00.500",
                    ["Outbox:MaxRetryDelay"] = "00:02:00",
                    ["Outbox:ProcessedRetentionPeriod"] = "3.00:00:00",
                    ["Scheduler:PollingInterval"] = "00:00:00.300",
                    ["Scheduler:LockMaxPoolSize"] = "24",
                    ["Scheduler:DefaultHistoryCount"] = "250",
                    ["Scheduler:WorkerId"] = "worker-config-1",
                }
            )
            .Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRuthPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        var outbox = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
        var scheduler = provider.GetRequiredService<IOptions<SchedulerOptions>>().Value;

        Assert.Equal(TimeSpan.FromMilliseconds(250), outbox.PollingInterval);
        Assert.Equal(12, outbox.BatchSize);
        Assert.Equal(TimeSpan.FromMinutes(1), outbox.LockDuration);
        Assert.Equal(9, outbox.MaxRetries);
        Assert.Equal(TimeSpan.FromMilliseconds(500), outbox.BaseRetryDelay);
        Assert.Equal(TimeSpan.FromMinutes(2), outbox.MaxRetryDelay);
        Assert.Equal(TimeSpan.FromDays(3), outbox.ProcessedRetentionPeriod);

        Assert.Equal(TimeSpan.FromMilliseconds(300), scheduler.PollingInterval);
        Assert.Equal(24, scheduler.LockMaxPoolSize);
        Assert.Equal(250, scheduler.DefaultHistoryCount);
        Assert.Equal("worker-config-1", scheduler.WorkerId);
    }

    [Fact]
    public void AddRuthPersistence_WithoutIConfigurationInDI_ResolvesDefaultOptionsWithoutThrowing()
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRuthPersistence(DummyConnectionString);

        using var provider = services.BuildServiceProvider();
        var outbox = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
        var scheduler = provider.GetRequiredService<IOptions<SchedulerOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(5), outbox.PollingInterval);
        Assert.Equal(TimeSpan.FromDays(7), outbox.ProcessedRetentionPeriod);
        Assert.Equal(TimeSpan.FromSeconds(15), scheduler.PollingInterval);
        Assert.Equal(500, scheduler.DefaultHistoryCount);
    }

    [Fact]
    public void AddRuthPersistence_WithConnectionStringAndDiConfiguration_BindsAndAppliesCallbackOverrides()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Outbox:BatchSize"] = "25",
                    ["Outbox:MaxRetries"] = "7",
                    ["Scheduler:PollingInterval"] = "00:00:00.400",
                    ["Scheduler:WorkerId"] = "from-di-config",
                }
            )
            .Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddRuthPersistence(
            DummyConnectionString,
            configureOutbox: opts => opts.MaxRetries = 11,
            configureScheduler: opts => opts.WorkerId = "from-callback"
        );

        using var provider = services.BuildServiceProvider();
        var outbox = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
        var scheduler = provider.GetRequiredService<IOptions<SchedulerOptions>>().Value;

        Assert.Equal(25, outbox.BatchSize);
        Assert.Equal(11, outbox.MaxRetries);
        Assert.Equal(TimeSpan.FromMilliseconds(400), scheduler.PollingInterval);
        Assert.Equal("from-callback", scheduler.WorkerId);
    }

    [Fact]
    public void AddRuthPersistence_RegistersBootstrapBeforeEngineAndOutboxRetentionJob()
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRuthPersistence(DummyConnectionString);

        using var provider = services.BuildServiceProvider();
        var hostedTypes = provider
            .GetServices<IHostedService>()
            .Select(static service => service.GetType())
            .ToList();

        var bootstrapIndex = hostedTypes.IndexOf(typeof(SchedulerBootstrapHostedService));
        var engineIndex = hostedTypes.IndexOf(typeof(ScheduledJobEngine));
        var outboxIndex = hostedTypes.IndexOf(typeof(OutboxDispatcherService));

        Assert.True(bootstrapIndex >= 0);
        Assert.True(engineIndex > bootstrapIndex);
        Assert.True(outboxIndex > bootstrapIndex);

        var jobs = provider.GetServices<IScheduledJob>().ToList();
        _ = Assert.Single(jobs, static job => job is OutboxRetentionCleanupJob);
    }
}
