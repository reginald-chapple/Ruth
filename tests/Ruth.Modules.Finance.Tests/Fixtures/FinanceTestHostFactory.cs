using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Modules.Communication;
using Ruth.Modules.Core;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Crm;
using Ruth.Modules.Groups;
using Ruth.Persistence.DependencyInjection;
using Ruth.Providers.CommunicationTransports;
using Ruth.Providers.PaymentGateways;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Finance.Tests.Fixtures;

internal static class FinanceTestHostFactory
{
    public static async Task<ServiceProvider> BuildAndMigrateAsync(
        string connectionString,
        Action<IServiceCollection>? configure = null
    )
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = connectionString,
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
            ["PaymentGateways:Nmi:WebhookSigningSecret"] = "nmi-test-webhook-secret",
            ["PaymentGateways:Stripe:WebhookSigningSecret"] = "stripe-test-webhook-secret",
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddCoreModule(configuration);
        var storage = new InMemoryStorageProvider();
        _ = services.AddSingleton(storage);
        _ = services.AddSingleton<IStorageProvider>(storage);
        _ = services.AddCrmModule(configuration);
        _ = services.AddGroupsModule(configuration);
        _ = services.AddCommunicationModule(configuration);
        _ = services.AddCommunicationTransportProviders(configuration);
        _ = services.AddPaymentGatewayProviders(configuration);
        _ = services.AddFinanceModule(configuration);
        configure?.Invoke(services);

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
