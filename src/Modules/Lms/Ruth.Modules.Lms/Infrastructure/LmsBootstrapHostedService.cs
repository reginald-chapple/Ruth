using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Lms.Infrastructure;

internal sealed partial class LmsBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<LmsBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<LmsDbContext>(cancellationToken);
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 6701,
        Level = LogLevel.Information,
        Message = "Applying LMS schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 6702,
        Level = LogLevel.Information,
        Message = "LMS schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
