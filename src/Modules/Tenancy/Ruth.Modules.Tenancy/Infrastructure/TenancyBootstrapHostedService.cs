using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure.Security;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Infrastructure;

internal sealed partial class TenancyBootstrapHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IOptions<TenancyOptions> options,
    IClock clock,
    ILogger<TenancyBootstrapHostedService> logger
) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IConfiguration _configuration = configuration
        ?? throw new ArgumentNullException(nameof(configuration));
    private readonly TenancyOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ILogger<TenancyBootstrapHostedService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(_logger);
        await _serviceProvider.MigrateModuleSchemaAsync<TenancyDbContext>(cancellationToken);
        await SeedOperatorIfConfiguredAsync(cancellationToken);
        await SeedActiveTenantsAsync(cancellationToken);
        LogMigrationsCompleted(_logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task SeedOperatorIfConfiguredAsync(CancellationToken cancellationToken)
    {
        if (
            string.IsNullOrWhiteSpace(_options.SeedOperatorEmail)
            || string.IsNullOrWhiteSpace(_options.SeedOperatorPassword)
        )
        {
            return;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        var normalizedEmail = _options.SeedOperatorEmail.Trim().ToLowerInvariant();

        var exists = await dbContext
            .PlatformOperators.AsNoTracking()
            .AnyAsync(x => x.EmailNormalized == normalizedEmail, cancellationToken);
        if (exists)
        {
            return;
        }

        var hash = PlatformOperatorPasswordHasher.HashPassword(_options.SeedOperatorPassword);
        _ = dbContext.PlatformOperators.Add(
            PlatformOperator.Create(
                normalizedEmail,
                _options.SeedOperatorDisplayName,
                hash,
                _clock.UtcNow
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedActiveTenantsAsync(CancellationToken cancellationToken)
    {
        var registry = _serviceProvider.GetService<IActiveTenantRegistry>();
        if (registry is null)
        {
            return;
        }

        var tenantIds = await registry.GetActiveTenantIdsAsync(cancellationToken);
        foreach (var tenantId in tenantIds)
        {
            using (AmbientTenantContext.BeginScope(tenantId))
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var contributors = scope.ServiceProvider.GetServices<ITenantSeedContributor>();
                foreach (var contributor in contributors)
                {
                    await contributor.SeedTenantAsync(tenantId, cancellationToken);
                }
            }
        }
    }

    [LoggerMessage(
        EventId = 3051,
        Level = LogLevel.Information,
        Message = "Applying Tenancy platform schema EF Core migrations."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 3052,
        Level = LogLevel.Information,
        Message = "Tenancy platform schema EF Core migrations completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
