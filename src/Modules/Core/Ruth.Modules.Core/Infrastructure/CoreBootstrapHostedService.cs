using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Core.Infrastructure;

internal sealed partial class CoreBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IEnumerable<EntityTypeRegistration> registrations,
    ICoreReadCache readCache,
    ILogger<CoreBootstrapHostedService> logger
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
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SyncEntityTypesAsync(dbContext, registrations, cancellationToken);
        await readCache.InvalidateAllAsync(cancellationToken);
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal static async Task SyncEntityTypesAsync(
        CoreDbContext dbContext,
        IEnumerable<EntityTypeRegistration> registrations,
        CancellationToken cancellationToken
    )
    {
        var existingByKey = await dbContext.EntityTypeDescriptors.ToDictionaryAsync(
            static e => e.Key,
            StringComparer.OrdinalIgnoreCase,
            cancellationToken
        );

        foreach (var registration in registrations)
        {
            if (existingByKey.TryGetValue(registration.Key, out var existing))
            {
                existing.SyncFrom(registration);
                continue;
            }

            var created = EntityTypeDescriptor.FromRegistration(registration);
            if (created.IsSuccess)
            {
                _ = dbContext.EntityTypeDescriptors.Add(created.Value);
                existingByKey[registration.Key] = created.Value;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Information,
        Message = "Applying Core schema EF Core migrations and synchronizing entity type registry."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 4102,
        Level = LogLevel.Information,
        Message = "Core schema migrations and entity type synchronization completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
