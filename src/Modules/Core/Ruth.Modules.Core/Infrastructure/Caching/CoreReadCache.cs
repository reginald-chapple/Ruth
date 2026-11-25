using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Ruth.Modules.Core.Infrastructure.Caching;

internal sealed partial class CoreReadCache(
    ILogger<CoreReadCache> logger,
    IConnectionMultiplexer? redis = null
) : ICoreReadCache
{
    public const string EntityTypesBucket = "entity-types";
    public const string DefinedTypesBucket = "defined-types";
    public const string CategoriesBucket = "categories";
    public const string AttributesBucket = "attributes";

    private readonly ConcurrentDictionary<string, CachedBucket> _buckets = new(
        StringComparer.OrdinalIgnoreCase
    );

    public async Task<T> GetOrLoadAsync<T>(
        string bucket,
        Func<CancellationToken, Task<T>> loader,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentNullException.ThrowIfNull(loader);

        var remoteVersion = await TryReadVersionAsync(bucket);
        if (
            _buckets.TryGetValue(bucket, out var existing)
            && existing.Payload is T typed
            && (
                remoteVersion is null
                || string.Equals(existing.Version, remoteVersion, StringComparison.Ordinal)
            )
        )
        {
            return typed;
        }

        var loaded = await loader(cancellationToken);
        var version = remoteVersion ?? Guid.CreateVersion7().ToString("N");
        _buckets[bucket] = new CachedBucket(version, loaded);
        return loaded;
    }

    public async Task InvalidateBucketAsync(
        string bucket,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _buckets.TryRemove(bucket, out _);
        await TryBumpVersionAsync(bucket);
    }

    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _buckets.Clear();
        await TryBumpVersionAsync(EntityTypesBucket);
        await TryBumpVersionAsync(DefinedTypesBucket);
        await TryBumpVersionAsync(CategoriesBucket);
        await TryBumpVersionAsync(AttributesBucket);
    }

    private async Task<string?> TryReadVersionAsync(string bucket)
    {
        if (redis is null || !redis.IsConnected)
        {
            return null;
        }

        try
        {
            var value = await redis.GetDatabase().StringGetAsync(BuildRedisKey(bucket));
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException ex)
        {
            LogRedisFallback(logger, bucket, ex);
            return null;
        }
    }

    private async Task TryBumpVersionAsync(string bucket)
    {
        if (redis is null || !redis.IsConnected)
        {
            return;
        }

        try
        {
            _ = await redis
                .GetDatabase()
                .StringSetAsync(BuildRedisKey(bucket), Guid.CreateVersion7().ToString("N"));
        }
        catch (RedisException ex)
        {
            LogRedisFallback(logger, bucket, ex);
        }
    }

    private static string BuildRedisKey(string bucket)
    {
        return $"ruth:core:cache:{bucket}:version";
    }

    [LoggerMessage(
        EventId = 4201,
        Level = LogLevel.Warning,
        Message = "Redis unavailable for Core cache bucket '{Bucket}'; using local in-memory cache."
    )]
    private static partial void LogRedisFallback(
        ILogger logger,
        string bucket,
        Exception exception
    );

    private sealed record CachedBucket(string Version, object Payload);
}
