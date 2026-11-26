using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Infrastructure;

internal sealed partial class CommunicationBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IClock clock,
    ILogger<CommunicationBootstrapHostedService> logger
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
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await BuiltInSystemCommunicationsSeeder.SeedAsync(
            dbContext,
            clock.UtcNow,
            cancellationToken
        );
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 5301,
        Level = LogLevel.Information,
        Message = "Applying Communication schema EF Core migrations and built-in notifications."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5302,
        Level = LogLevel.Information,
        Message = "Communication schema migrations and seeding completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
