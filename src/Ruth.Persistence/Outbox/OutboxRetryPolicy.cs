namespace Ruth.Persistence.Outbox;

/// <summary>Pure retry and dead-letter calculations for transactional Outbox delivery.</summary>
public static class OutboxRetryPolicy
{
    /// <summary>Computes a bounded exponential backoff delay for <paramref name="retryAttempt" /> (1-based).</summary>
    public static TimeSpan ComputeBackoffDelay(
        int retryAttempt,
        TimeSpan baseDelay,
        TimeSpan maxDelay
    )
    {
        if (retryAttempt < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retryAttempt),
                retryAttempt,
                "Retry attempt must be at least 1."
            );
        }

        if (baseDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseDelay),
                baseDelay,
                "Base delay cannot be negative."
            );
        }

        if (maxDelay < baseDelay)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDelay),
                maxDelay,
                "Max delay cannot be smaller than base delay."
            );
        }

        var exponent = Math.Min(retryAttempt - 1, 30);
        var multiplier = Math.Pow(2, exponent);
        var scaledTicks = baseDelay.Ticks * multiplier;

        return scaledTicks >= maxDelay.Ticks || double.IsInfinity(scaledTicks)
            ? maxDelay
            : TimeSpan.FromTicks((long)scaledTicks);
    }

    /// <summary>Determines whether a message has exhausted its retry budget and must be dead-lettered.</summary>
    public static bool ShouldDeadLetter(int nextRetryCount, int maxRetries)
    {
        return maxRetries <= 0 || nextRetryCount >= maxRetries;
    }
}
