using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ruth.Modules.Identity.Infrastructure;

internal sealed partial class IdentityBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<IdentityBootstrapHostedService> logger
) : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
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
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
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
