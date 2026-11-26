using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ruth.Modules.Commerce.Infrastructure;

internal sealed partial class CommerceBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CommerceBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 5901,
        Level = LogLevel.Information,
        Message = "Applying Commerce schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5902,
        Level = LogLevel.Information,
        Message = "Commerce schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
