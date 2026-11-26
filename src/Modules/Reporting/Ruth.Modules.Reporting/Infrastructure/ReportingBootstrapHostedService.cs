using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Reporting.Infrastructure.Analytics;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Reporting.Infrastructure;

internal sealed partial class ReportingBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<ReportingBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<ReportingDbContext>(cancellationToken);
        var provisioner = serviceProvider.GetService<PostgresRlsRoleProvisioner>();
        await StarSchemaSqlViews.EnsureBiViewsAsync(
            provisioner,
            fallbackDb: null,
            cancellationToken
        );
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 6501,
        Level = LogLevel.Information,
        Message = "Applying Reporting schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 6502,
        Level = LogLevel.Information,
        Message = "Reporting schema migrations and BI views completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
