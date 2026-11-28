using System.Collections.Concurrent;

namespace Ruth.SharedKernel.Security;

/// <summary>
/// Bounded in-memory deduplication guard rejecting replayed webhook deliveries within the active replay window.
/// </summary>
public sealed class WebhookReplayDeduplicator
{
    private const int PruneThreshold = 512;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new(
        StringComparer.Ordinal
    );
    private int _insertCount;

    /// <summary>
    /// Attempts to record a unique webhook delivery fingerprint for <paramref name="ttl" />.
    /// Returns <see langword="false" /> when the same delivery was already accepted inside the active window.
    /// </summary>
    public bool TryRegisterDelivery(
        string scope,
        string deliveryKey,
        DateTimeOffset utcNow,
        TimeSpan? ttl = null
    )
    {
        if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(deliveryKey))
        {
            return false;
        }

        var now = utcNow.ToUniversalTime();
        var effectiveTtl = ttl ?? WebhookHmacReplayVerifier.DefaultReplayTolerance;
        var expiresAt = now.Add(effectiveTtl);
        var compositeKey = $"{scope}:{deliveryKey}";

        if (Interlocked.Increment(ref _insertCount) % PruneThreshold == 0)
        {
            PruneExpired(now);
        }

        while (true)
        {
            if (_seen.TryGetValue(compositeKey, out var existingExpiry))
            {
                if (existingExpiry > now)
                {
                    return false;
                }

                _ = _seen.TryUpdate(compositeKey, expiresAt, existingExpiry);
                return true;
            }

            if (_seen.TryAdd(compositeKey, expiresAt))
            {
                return true;
            }
        }
    }

    private void PruneExpired(DateTimeOffset nowUtc)
    {
        foreach (var entry in _seen)
        {
            if (entry.Value <= nowUtc)
            {
                _ = _seen.TryRemove(entry.Key, out _);
            }
        }
    }
}
