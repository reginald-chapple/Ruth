using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ruth.Modules.Groups.Infrastructure;

internal sealed partial class GroupsBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<GroupsBootstrapHostedService> logger
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
        var dbContext = scope.ServiceProvider.GetRequiredService<GroupsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 5201,
        Level = LogLevel.Information,
        Message = "Applying Groups schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5202,
        Level = LogLevel.Information,
        Message = "Groups schema migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
