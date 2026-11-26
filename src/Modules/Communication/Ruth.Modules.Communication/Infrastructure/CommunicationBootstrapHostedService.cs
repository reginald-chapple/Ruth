using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication.Infrastructure;

internal sealed partial class CommunicationBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<CommunicationBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<CommunicationDbContext>(cancellationToken);

        if (AmbientTenantContext.Instance.HasTenant)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var seeder =
                scope.ServiceProvider.GetRequiredService<CommunicationTenantSeedContributor>();
            await seeder.SeedTenantAsync(AmbientTenantContext.CurrentTenantId, cancellationToken);
        }

        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 5301,
        Level = LogLevel.Information,
        Message = "Applying Communication schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5302,
        Level = LogLevel.Information,
        Message = "Communication schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
