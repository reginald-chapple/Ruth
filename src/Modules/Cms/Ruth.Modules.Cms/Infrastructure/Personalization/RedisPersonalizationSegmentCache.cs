using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Tenancy;
using StackExchange.Redis;

namespace Ruth.Modules.Cms.Infrastructure.Personalization;

internal sealed partial class RedisPersonalizationSegmentCache(
    ILogger<RedisPersonalizationSegmentCache> logger,
    IConnectionMultiplexer? redis = null
) : IPersonalizationSegmentCache
{
    private const long BitsetSize = 262_144;

    private readonly ConcurrentDictionary<string, HashSet<Guid>> _localSets = new(
        StringComparer.OrdinalIgnoreCase
    );

    public async Task SetSegmentMembersAsync(
        string segmentKey,
        IReadOnlyCollection<Guid> personIds,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
        {
            return;
        }

        var localCopy = new HashSet<Guid>(personIds);
        _localSets[BuildLocalKey(tenantId, segmentKey)] = localCopy;

        if (redis is null || !redis.IsConnected)
        {
            return;
        }

        try
        {
            var db = redis.GetDatabase();
            var setKey = BuildSetKey(tenantId, segmentKey);
            var bitKey = BuildBitsetKey(tenantId, segmentKey);

            _ = await db.KeyDeleteAsync([setKey, bitKey]);
            if (personIds.Count > 0)
            {
                var values = personIds.Select(static id => (RedisValue)id.ToString("D")).ToArray();
                _ = await db.SetAddAsync(setKey, values);
                foreach (var personId in personIds)
                {
                    _ = await db.StringSetBitAsync(bitKey, ComputeBitOffset(personId), bit: true);
                }
            }
        }
        catch (RedisException ex)
        {
            LogRedisFallback(logger, segmentKey, ex);
        }
    }

    public async Task<bool?> IsMemberAsync(
        string segmentKey,
        Guid personId,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
        {
            return false;
        }

        if (redis is not null && redis.IsConnected)
        {
            try
            {
                var db = redis.GetDatabase();
                var bitKey = BuildBitsetKey(tenantId, segmentKey);
                var setKey = BuildSetKey(tenantId, segmentKey);
                if (await db.KeyExistsAsync(setKey))
                {
                    return await db.StringGetBitAsync(bitKey, ComputeBitOffset(personId))
                        && await db.SetContainsAsync(setKey, personId.ToString("D"));
                }
            }
            catch (RedisException ex)
            {
                LogRedisFallback(logger, segmentKey, ex);
            }
        }

        return _localSets.TryGetValue(BuildLocalKey(tenantId, segmentKey), out var members)
            ? members.Contains(personId)
            : null;
    }

    internal static long ComputeBitOffset(Guid personId)
    {
        Span<byte> bytes = stackalloc byte[16];
        _ = personId.TryWriteBytes(bytes);
        Span<byte> hash = stackalloc byte[32];
        _ = SHA256.HashData(bytes, hash);
        var raw = BinaryPrimitives.ReadUInt64LittleEndian(hash);
        return (long)(raw % BitsetSize);
    }

    private static string BuildLocalKey(TenantId tenantId, string segmentKey)
    {
        return $"{tenantId}:{segmentKey}";
    }

    private static RedisKey BuildSetKey(TenantId tenantId, string segmentKey)
    {
        return $"t:{tenantId}:cms:segment:{segmentKey}:members";
    }

    private static RedisKey BuildBitsetKey(TenantId tenantId, string segmentKey)
    {
        return $"t:{tenantId}:cms:segment:{segmentKey}:bits";
    }

    [LoggerMessage(
        EventId = 6303,
        Level = LogLevel.Warning,
        Message = "Redis unavailable for CMS personalization segment '{SegmentKey}'; falling back to local cache."
    )]
    private static partial void LogRedisFallback(
        ILogger logger,
        string segmentKey,
        Exception exception
    );
}
