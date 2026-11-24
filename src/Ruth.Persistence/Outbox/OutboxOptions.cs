namespace Ruth.Persistence.Outbox;

/// <summary>Configuration options for <see cref="OutboxDispatcherService" />.</summary>
public sealed class OutboxOptions
{
    /// <summary>Gets or sets the polling interval between Outbox sweeps.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the maximum number of messages claimed per module batch.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>Gets or sets the worker lease duration applied during batch claiming.</summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the maximum delivery attempts before dead-lettering a message.</summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>Gets or sets the initial exponential backoff delay after the first failure.</summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Gets or sets the upper bound for exponential backoff delays.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets the retention period for processed Outbox messages before pruning.</summary>
    public TimeSpan ProcessedRetentionPeriod { get; set; } = TimeSpan.FromDays(7);
}
