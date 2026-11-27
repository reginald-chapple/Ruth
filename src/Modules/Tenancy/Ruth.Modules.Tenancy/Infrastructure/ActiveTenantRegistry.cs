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

    private IReadOnlyList<TenantExecutionState>? _cachedStates;
    private IReadOnlyList<TenantId>? _cachedIds;
    private DateTimeOffset _expiresAtUtc;

    public async ValueTask<IReadOnlyList<TenantId>> GetActiveTenantIdsAsync(
        CancellationToken cancellationToken = default
    )
    {
        _ = await GetActiveTenantsAsync(cancellationToken);
        lock (_gate)
        {
            return _cachedIds ?? [];
        }
    }

    public async ValueTask<IReadOnlyList<TenantExecutionState>> GetActiveTenantsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var nowUtc = _clock.UtcNow;
        lock (_gate)
        {
            if (_cachedStates is not null && nowUtc < _expiresAtUtc)
            {
                return _cachedStates;
            }
        }

        var states = await LoadStatesFromDatabaseAsync(cancellationToken);
        TenantId[] ids = [.. states.Select(static s => s.TenantId)];

        lock (_gate)
        {
            _cachedStates = states;
            _cachedIds = ids;
            _expiresAtUtc = _clock.UtcNow + _cacheTtl;
        }

        return states;
    }

    public void Invalidate()
    {
        lock (_gate)
        {
            _cachedStates = null;
            _cachedIds = null;
            _expiresAtUtc = DateTimeOffset.MinValue;
        }
    }

    private async Task<List<TenantExecutionState>> LoadStatesFromDatabaseAsync(
        CancellationToken cancellationToken
    )
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        var rows = await dbContext
            .Tenants.AsNoTracking()
            .OrderBy(static t => t.CreatedOnUtc)
            .Select(static t => new
            {
                t.Id,
                t.Slug,
                t.LifecycleStage,
            })
            .ToListAsync(cancellationToken);

        var states = new List<TenantExecutionState>(rows.Count + 1);
        foreach (var row in rows)
        {
            if (row.LifecycleStage != TenantLifecycleStage.Suspended)
            {
                states.Add(new TenantExecutionState(row.Id, row.Slug, row.LifecycleStage));
            }
        }

        if (_options.DedicatedWorkspaceTenantId is { } dedicated && dedicated != Guid.Empty)
        {
            var dedicatedId = TenantId.From(dedicated);
            if (!rows.Exists(r => r.Id == dedicatedId))
            {
                states.Add(
                    new TenantExecutionState(
                        dedicatedId,
                        "dedicated-workspace",
                        TenantLifecycleStage.Live
                    )
                );
            }
        }

        return states;
    }
}
