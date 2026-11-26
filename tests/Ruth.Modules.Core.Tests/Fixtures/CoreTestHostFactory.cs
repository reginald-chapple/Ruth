using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Persistence.DependencyInjection;
using Ruth.Providers.RustFsStorage;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Core.Tests.Fixtures;

internal static class CoreTestHostFactory
{
    public static Task<ServiceProvider> BuildAndMigrateAsync(string connectionString)
    {
        return BuildAndMigrateAsync(
            connectionString,
            rustFsEndpoint: null,
            configureServices: null
        );
    }

    public static async Task<ServiceProvider> BuildAndMigrateAsync(
        string connectionString,
        string? rustFsEndpoint,
        Action<IServiceCollection>? configureServices = null
    )
    {
        AmbientTenantContext.SetTestHarnessDefault(TenantId.Create(), TenantLifecycleStage.Live);
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = connectionString,
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
        };
        if (!string.IsNullOrWhiteSpace(rustFsEndpoint))
        {
            values["Storage:RustFs:Endpoint"] = rustFsEndpoint;
            values["Storage:RustFs:BucketName"] = $"ruth-test-{Guid.NewGuid():N}"[..24];
            values["Storage:RustFs:AccessKey"] = CorePostgresContainerFixture.RustFsAccessKey;
            values["Storage:RustFs:SecretKey"] = CorePostgresContainerFixture.RustFsSecretKey;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        if (!string.IsNullOrWhiteSpace(rustFsEndpoint))
        {
            _ = services.AddRustFsStorageProvider(configuration);
        }

        _ = services.AddCoreModule(configuration);
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();
        foreach (var hosted in provider.GetServices<IHostedService>())
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        return provider;
    }
}
