namespace Ruth.Persistence.Outbox;

/// <summary>Transactional Outbox record persisted atomically alongside module domain changes.</summary>
public sealed class OutboxMessage
{
    private OutboxMessage() { }

    /// <summary>Gets the UUIDv7 identifier of the integration event / outbox message.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the logical or assembly-qualified event type key.</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Gets the JSONB-serialized integration event payload.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>Gets the UTC instant when the event occurred.</summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }

    /// <summary>Gets the UTC instant when the message was successfully dispatched, or <see langword="null" />.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    /// <summary>Gets the UTC instant when the message was permanently dead-lettered, or <see langword="null" />.</summary>
    public DateTimeOffset? DeadLetteredOnUtc { get; private set; }

    /// <summary>Gets the number of failed dispatch attempts.</summary>
    public int RetryCount { get; private set; }

    /// <summary>Gets the earliest UTC instant for the next retry attempt, or <see langword="null" />.</summary>
    public DateTimeOffset? NextRetryOnUtc { get; private set; }

    /// <summary>Gets the UTC lease expiration timestamp while claimed by a dispatcher worker.</summary>
    public DateTimeOffset? LockedUntilUtc { get; private set; }

    /// <summary>Gets the per-claim lease ownership token while claimed by a dispatcher worker.</summary>
    public Guid? LockToken { get; private set; }

    /// <summary>Gets the truncated diagnostic error message from the last failed attempt.</summary>
    public string? Error { get; private set; }

    /// <summary>Creates a new pending <see cref="OutboxMessage" />.</summary>
    public static OutboxMessage Create(
        Guid id,
        string eventType,
        string payload,
        DateTimeOffset occurredOnUtc
    )
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Outbox message ID cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage
        {
            Id = id,
            EventType = eventType,
            Payload = payload,
            OccurredOnUtc = occurredOnUtc.ToUniversalTime(),
            RetryCount = 0,
        };
    }
}
