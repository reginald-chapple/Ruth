using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Search.Infrastructure.Querying;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Search.Infrastructure;

internal sealed partial class SearchBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<SearchBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await serviceProvider.MigrateModuleSchemaAsync<SearchDbContext>(cancellationToken);
        var provisioner = serviceProvider.GetService<PostgresRlsRoleProvisioner>();
        await SearchPostgresIndexInitializer.EnsureFullTextIndexesAsync(
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
        EventId = 6801,
        Level = LogLevel.Information,
        Message = "Applying Search schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 6802,
        Level = LogLevel.Information,
        Message = "Search schema migrations and PostgreSQL tsvector/pg_trgm indexes completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
