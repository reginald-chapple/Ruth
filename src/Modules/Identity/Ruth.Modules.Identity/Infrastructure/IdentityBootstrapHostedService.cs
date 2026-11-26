using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Identity.Infrastructure;

internal sealed partial class IdentityBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<IdentityBootstrapHostedService> logger
) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IConfiguration _configuration = configuration
        ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<IdentityBootstrapHostedService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(_logger);
        await _serviceProvider.MigrateModuleSchemaAsync<IdentityDbContext>(cancellationToken);
        LogMigrationsApplied(_logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "Applying Identity schema EF Core migrations because Bootstrap:AutoMigrateAndSeed is enabled."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Information,
        Message = "Identity schema EF Core migrations completed."
    )]
    private static partial void LogMigrationsApplied(ILogger logger);
}
