using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Identity.Domain;
using StackExchange.Redis;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal sealed partial class AclSnapshotCache(
    IServiceScopeFactory scopeFactory,
    ILogger<AclSnapshotCache> logger,
    IConnectionMultiplexer? redis = null
) : IAclSnapshotCache, IDisposable
{
    private const string RedisVersionKey = "ruth:identity:acl:version";

    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private volatile CachedSnapshotState? _current;

    public async ValueTask<AclRuleSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default
    )
    {
        var remoteVersion = await TryReadRedisVersionAsync();
        var cached = _current;
        if (
            cached is not null
            && (
                remoteVersion is null
                || string.Equals(cached.Version, remoteVersion, StringComparison.Ordinal)
            )
        )
        {
            return cached.Snapshot;
        }

        await _reloadGate.WaitAsync(cancellationToken);
        try
        {
            cached = _current;
            if (
                cached is not null
                && (
                    remoteVersion is null
                    || string.Equals(cached.Version, remoteVersion, StringComparison.Ordinal)
                )
            )
            {
                return cached.Snapshot;
            }

            var snapshot = await LoadFromDatabaseAsync(cancellationToken);
            var version = remoteVersion ?? Guid.CreateVersion7().ToString("N");
            _current = new CachedSnapshotState(version, snapshot);
            return snapshot;
        }
        finally
        {
            _ = _reloadGate.Release();
        }
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        _current = null;
        await TryBumpRedisVersionAsync();
        LogAclCacheInvalidated(logger);
    }

    public void Dispose()
    {
        _reloadGate.Dispose();
    }

    private async Task<AclRuleSnapshot> LoadFromDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var rules = await dbContext.EntityAclRules.AsNoTracking().ToListAsync(cancellationToken);
        var activeRoleIds = await dbContext
            .SecurityRoles.AsNoTracking()
            .Where(static r => r.IsActive)
            .Select(static r => r.Id.Value)
            .ToListAsync(cancellationToken);

        return AclRuleSnapshot.Create(rules, activeRoleIds);
    }

    private async Task<string?> TryReadRedisVersionAsync()
    {
        if (redis is null || !redis.IsConnected)
        {
            return null;
        }

        try
        {
            var value = await redis.GetDatabase().StringGetAsync(RedisVersionKey);
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException ex)
        {
            LogRedisFallback(logger, ex);
            return null;
        }
    }

    private async Task TryBumpRedisVersionAsync()
    {
        if (redis is null || !redis.IsConnected)
        {
            return;
        }

        try
        {
            var newVersion = Guid.CreateVersion7().ToString("N");
            _ = await redis.GetDatabase().StringSetAsync(RedisVersionKey, newVersion);
        }
        catch (RedisException ex)
        {
            LogRedisFallback(logger, ex);
        }
    }

    [LoggerMessage(
        EventId = 3201,
        Level = LogLevel.Debug,
        Message = "Invalidated cached ACL rule snapshot."
    )]
    private static partial void LogAclCacheInvalidated(ILogger logger);

    [LoggerMessage(
        EventId = 3202,
        Level = LogLevel.Warning,
        Message = "Redis unavailable while coordinating ACL cache version; using local in-memory snapshot."
    )]
    private static partial void LogRedisFallback(ILogger logger, Exception exception);

    private sealed record CachedSnapshotState(string Version, AclRuleSnapshot Snapshot);
}
