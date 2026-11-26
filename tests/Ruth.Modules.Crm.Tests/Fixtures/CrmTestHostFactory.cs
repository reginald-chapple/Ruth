using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Modules.Core;
using Ruth.Persistence.DependencyInjection;
using Ruth.Providers.Checkr;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Crm.Tests.Fixtures;

internal static class CrmTestHostFactory
{
    public static async Task<ServiceProvider> BuildAndMigrateAsync(
        string connectionString,
        bool includeCoreModule = false,
        string? checkrWebhookSecret = null
    )
    {
        AmbientTenantContext.SetTestHarnessDefault(TenantId.Create(), TenantLifecycleStage.Live);
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = connectionString,
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
        };
        if (!string.IsNullOrWhiteSpace(checkrWebhookSecret))
        {
            values["BackgroundChecks:Checkr:WebhookSecret"] = checkrWebhookSecret;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        if (includeCoreModule)
        {
            _ = services.AddCoreModule(configuration);
        }

        _ = services.AddCheckrBackgroundCheckProvider(configuration);
        _ = services.AddCrmModule(configuration);

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
