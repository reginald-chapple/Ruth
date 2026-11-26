using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Modules.Communication;
using Ruth.Modules.Core;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Crm;
using Ruth.Modules.Finance;
using Ruth.Modules.Groups;
using Ruth.Persistence.DependencyInjection;
using Ruth.Providers.CommunicationTransports;
using Ruth.Providers.PaymentGateways;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Events.Tests.Fixtures;

internal static class EventsTestHostFactory
{
    public static async Task<ServiceProvider> BuildAndMigrateAsync(
        string connectionString,
        Action<IServiceCollection>? configure = null
    )
    {
        AmbientTenantContext.SetTestHarnessDefault(TenantId.Create(), TenantLifecycleStage.Live);

        var configuration = BuildConfiguration(connectionString);
        var services = new ServiceCollection();
        RegisterModules(services, configuration);
        configure?.Invoke(services);

        var provider = services.BuildServiceProvider();
        foreach (var hosted in provider.GetServices<IHostedService>())
        {
            if (hosted is not BackgroundService)
            {
                await hosted.StartAsync(CancellationToken.None);
            }
        }

        return provider;
    }

    private static IConfigurationRoot BuildConfiguration(string connectionString)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = connectionString,
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
            ["PaymentGateways:Nmi:WebhookSigningSecret"] = "nmi-test-webhook-secret",
            ["PaymentGateways:Stripe:WebhookSigningSecret"] = "stripe-test-webhook-secret",
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static void RegisterModules(IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddLogging();
        _ = services.AddSingleton(configuration);
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddCoreModule(configuration);
        _ = services.AddSingleton<IStorageProvider>(new EventsInMemoryStorageProvider());
        _ = services.AddCrmModule(configuration);
        _ = services.AddGroupsModule(configuration);
        _ = services.AddCommunicationModule(configuration);
        _ = services.AddCommunicationTransportProviders(configuration);
        _ = services.AddPaymentGatewayProviders(configuration);
        _ = services.AddFinanceModule(configuration);
        _ = services.AddEventsModule(configuration);
    }
}
