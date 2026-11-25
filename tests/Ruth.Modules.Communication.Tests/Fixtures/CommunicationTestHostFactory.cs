using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Communication.Tests.Fixtures;

internal static class CommunicationTestHostFactory
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
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddCommunicationModule(configuration);
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
