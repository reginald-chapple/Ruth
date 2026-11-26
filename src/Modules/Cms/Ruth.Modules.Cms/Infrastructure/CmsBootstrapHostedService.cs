using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Cms.Infrastructure;

internal sealed partial class CmsBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<CmsBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<CmsDbContext>(cancellationToken);

        if (AmbientTenantContext.Instance.HasTenant)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<CmsTenantSeedContributor>();
            await seeder.SeedTenantAsync(AmbientTenantContext.CurrentTenantId, cancellationToken);
        }

        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal static Task EnsureBuiltInBlockTypesAsync(
        CmsDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        return CmsTenantSeedContributor.EnsureBuiltInBlockTypesAsync(dbContext, cancellationToken);
    }

    [LoggerMessage(
        EventId = 6101,
        Level = LogLevel.Information,
        Message = "Applying Cms schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 6102,
        Level = LogLevel.Information,
        Message = "Cms schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
