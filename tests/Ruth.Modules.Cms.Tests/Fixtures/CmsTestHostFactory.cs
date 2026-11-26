using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Tenancy;
using StackExchange.Redis;

namespace Ruth.Modules.Cms.Tests.Fixtures;

internal static class CmsTestHostFactory
{
    public static async Task<ServiceProvider> CreateAsync(
        string connectionString,
        IConnectionMultiplexer? redis = null
    )
    {
        AmbientTenantContext.SetTestHarnessDefault(TenantId.Create(), TenantLifecycleStage.Live);

        var provider = BuildServiceProvider(connectionString, redis);
        await InitializeDatabaseAsync(provider);
        return provider;
    }

    private static ServiceProvider BuildServiceProvider(
        string connectionString,
        IConnectionMultiplexer? redis
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:RuthDatabase"] = connectionString,
                    ["Bootstrap:AutoMigrateAndSeed"] = "false",
                    ["Cms:InteractionFlushWorkerEnabled"] = "false",
                }
            )
            .Build();

        var services = new ServiceCollection();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddLogging(static b => b.SetMinimumLevel(LogLevel.Warning));
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);

        if (redis is not null)
        {
            _ = services.AddSingleton(redis);
        }

        _ = services.AddCmsModule(configuration);
        return services.BuildServiceProvider();
    }

    private static async Task InitializeDatabaseAsync(ServiceProvider provider)
    {
        await provider.MigrateModuleSchemaAsync<CmsDbContext>();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        await CmsBootstrapHostedService.EnsureBuiltInBlockTypesAsync(db, CancellationToken.None);
    }
}
