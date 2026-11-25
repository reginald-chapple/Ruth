using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Identity.Tests.Fixtures;

internal static class IdentityTestHostFactory
{
    public static ServiceProvider BuildServiceProvider(
        string connectionString,
        bool autoMigrate,
        RecordingOnboardedConsumer? consumer = null,
        Action<IServiceCollection>? configureServices = null
    )
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
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddIdentityModule(configuration);

        if (consumer is not null)
        {
            _ = services.AddSingleton(consumer);
            _ = services.AddIdempotentIntegrationEventConsumer<
                OrganizationOnboardedIntegrationEvent,
                RecordingOnboardedConsumerAdapter,
                IdentityDbContext
            >();
        }

        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public static async Task StartBootstrapAsync(ServiceProvider provider)
    {
        foreach (
            var service in provider
                .GetServices<IHostedService>()
                .OfType<IdentityBootstrapHostedService>()
        )
        {
            await service.StartAsync(CancellationToken.None);
        }
    }

    public static CompleteOnboardingCommand CreateValidCommand(
        string userName = "admin",
        string email = "admin@civic.org",
        string password = "StrongPass#2026!"
    )
    {
        return new CompleteOnboardingCommand(
            "Civic Foundation",
            "UTC",
            "Main Branch",
            "MAIN",
            userName,
            email,
            "Civic Admin",
            password
        );
    }

    internal sealed class RecordingOnboardedConsumer
    {
        public List<OrganizationOnboardedIntegrationEvent> ReceivedEvents { get; } = [];
    }

    private sealed class RecordingOnboardedConsumerAdapter(RecordingOnboardedConsumer recorder)
        : IIntegrationEventConsumer<OrganizationOnboardedIntegrationEvent>
    {
        public Task ConsumeAsync(
            OrganizationOnboardedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            recorder.ReceivedEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
