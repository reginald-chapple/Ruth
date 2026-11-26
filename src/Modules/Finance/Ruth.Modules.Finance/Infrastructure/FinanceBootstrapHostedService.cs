using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Finance.Infrastructure;

internal sealed partial class FinanceBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<FinanceBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<FinanceDbContext>(cancellationToken);

        if (AmbientTenantContext.Instance.HasTenant)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<FinanceTenantSeedContributor>();
            await seeder.SeedTenantAsync(AmbientTenantContext.CurrentTenantId, cancellationToken);
        }

        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 5401,
        Level = LogLevel.Information,
        Message = "Applying Finance schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5402,
        Level = LogLevel.Information,
        Message = "Finance schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
