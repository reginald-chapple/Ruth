using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Crm.Infrastructure;

internal sealed partial class CrmBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<CrmBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<CrmDbContext>(cancellationToken);

        if (AmbientTenantContext.Instance.HasTenant)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<CrmTenantSeedContributor>();
            await seeder.SeedTenantAsync(AmbientTenantContext.CurrentTenantId, cancellationToken);
        }

        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal static Task SeedReferenceCatalogsAsync(
        CrmDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        return CrmTenantSeedContributor.SeedReferenceCatalogsAsync(
            dbContext,
            dbContext.CurrentTenantId,
            cancellationToken
        );
    }

    [LoggerMessage(
        EventId = 5103,
        Level = LogLevel.Information,
        Message = "Applying CRM schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5104,
        Level = LogLevel.Information,
        Message = "CRM schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
