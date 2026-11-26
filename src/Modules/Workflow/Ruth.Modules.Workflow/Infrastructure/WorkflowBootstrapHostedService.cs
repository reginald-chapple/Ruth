using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Workflow.Infrastructure;

internal sealed partial class WorkflowBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<WorkflowBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<WorkflowDbContext>(cancellationToken);
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 5601,
        Level = LogLevel.Information,
        Message = "Applying Workflow schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5602,
        Level = LogLevel.Information,
        Message = "Workflow schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
