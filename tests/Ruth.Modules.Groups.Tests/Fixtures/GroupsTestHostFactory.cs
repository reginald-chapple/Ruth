using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Modules.Core;
using Ruth.Modules.Crm;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Groups.Tests.Fixtures;

internal static class GroupsTestHostFactory
{
    public static async Task<ServiceProvider> BuildAndMigrateAsync(string connectionString)
    {
        AmbientTenantContext.SetTestHarnessDefault(TenantId.Create(), TenantLifecycleStage.Live);
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = connectionString,
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddCoreModule(configuration);
        _ = services.AddCrmModule(configuration);
        _ = services.AddGroupsModule(configuration);

        var provider = services.BuildServiceProvider();
        foreach (var hosted in provider.GetServices<IHostedService>())
        {
            if (hosted is BackgroundService)
            {
                continue;
            }

            await hosted.StartAsync(CancellationToken.None);
        }

        return provider;
    }
}
