namespace Ruth.Persistence.Outbox;

/// <summary>Processes pending transactional Outbox messages for a single module schema.</summary>
public interface IOutboxModuleProcessor
{
    /// <summary>Gets the target module schema name.</summary>
    string Schema { get; }

    /// <summary>Claims and dispatches one batch of pending Outbox messages.</summary>
    Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes processed Outbox messages older than <paramref name="cutoffUtc" />.</summary>
    Task<int> PruneProcessedAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default
    );
}
