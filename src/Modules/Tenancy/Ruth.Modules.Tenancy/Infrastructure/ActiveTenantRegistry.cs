using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Tenancy.Infrastructure;

internal sealed class ActiveTenantRegistry(
    IServiceScopeFactory scopeFactory,
    IOptions<TenancyOptions> options,
    IClock clock
) : IActiveTenantRegistry
{
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory
        ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly TenancyOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly Lock _gate = new();

    private IReadOnlyList<TenantId>? _cachedIds;
    private DateTimeOffset _expiresAtUtc;

    public async ValueTask<IReadOnlyList<TenantId>> GetActiveTenantIdsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var nowUtc = _clock.UtcNow;
        lock (_gate)
        {
            if (_cachedIds is not null && nowUtc < _expiresAtUtc)
            {
                return _cachedIds;
            }
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        var ids = await dbContext
            .Tenants.AsNoTracking()
            .Where(static t => t.LifecycleStage != TenantLifecycleStage.Suspended)
            .OrderBy(static t => t.CreatedOnUtc)
            .Select(static t => t.Id)
            .ToListAsync(cancellationToken);

        if (_options.DedicatedWorkspaceTenantId is { } dedicated && dedicated != Guid.Empty)
        {
            var dedicatedId = TenantId.From(dedicated);
            if (!ids.Contains(dedicatedId))
            {
                ids.Add(dedicatedId);
            }
        }

        lock (_gate)
        {
            _cachedIds = ids;
            _expiresAtUtc = _clock.UtcNow + _cacheTtl;
        }

        return ids;
    }

    public void Invalidate()
    {
        lock (_gate)
        {
            _cachedIds = null;
            _expiresAtUtc = DateTimeOffset.MinValue;
        }
    }
}
