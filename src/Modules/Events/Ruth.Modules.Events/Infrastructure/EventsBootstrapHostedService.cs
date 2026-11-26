using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Events.Infrastructure;

internal sealed partial class EventsBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<EventsBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<EventsDbContext>(cancellationToken);

        if (AmbientTenantContext.Instance.HasTenant)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<EventsTenantSeedContributor>();
            await seeder.SeedTenantAsync(AmbientTenantContext.CurrentTenantId, cancellationToken);
        }

        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 5701,
        Level = LogLevel.Information,
        Message = "Applying Events schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5702,
        Level = LogLevel.Information,
        Message = "Events schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
