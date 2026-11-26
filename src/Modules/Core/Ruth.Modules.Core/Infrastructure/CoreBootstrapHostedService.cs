using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Core.Infrastructure;

internal sealed partial class CoreBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<CoreBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<CoreDbContext>(cancellationToken);

        if (AmbientTenantContext.Instance.HasTenant)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<CoreTenantSeedContributor>();
            await seeder.SeedTenantAsync(AmbientTenantContext.CurrentTenantId, cancellationToken);
        }

        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal static Task SyncEntityTypesAsync(
        CoreDbContext dbContext,
        IEnumerable<SharedKernel.Metadata.EntityTypeRegistration> registrations,
        CancellationToken cancellationToken
    )
    {
        return CoreTenantSeedContributor.SyncEntityTypesAsync(
            dbContext,
            registrations,
            dbContext.CurrentTenantId,
            cancellationToken
        );
    }

    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Information,
        Message = "Applying Core schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 4102,
        Level = LogLevel.Information,
        Message = "Core schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
