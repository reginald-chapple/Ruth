using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Persistence.Scheduling;

internal sealed partial class SchedulerBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SchedulerBootstrapHostedService> logger,
    IConfiguration? configuration = null
) : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<SchedulerBootstrapHostedService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));
    private readonly IConfiguration? _configuration = configuration;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!(_configuration?.GetValue<bool>("Bootstrap:AutoMigrateAndSeed") ?? false))
        {
            return;
        }

        LogApplyingMigrations(_logger);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var provisioner = scope.ServiceProvider.GetService<PostgresRlsRoleProvisioner>();
        if (provisioner is not null)
        {
            await provisioner.LockDownControlPlaneSchemaAsync(
                SchedulerDbContext.SchemaName,
                cancellationToken
            );
        }

        LogMigrationsApplied(_logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Information,
        Message = "Applying Scheduler schema EF Core migrations because Bootstrap:AutoMigrateAndSeed is enabled."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 4102,
        Level = LogLevel.Information,
        Message = "Scheduler schema EF Core migrations completed."
    )]
    private static partial void LogMigrationsApplied(ILogger logger);
}
