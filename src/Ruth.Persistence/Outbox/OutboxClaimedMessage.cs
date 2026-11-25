namespace Ruth.Persistence.Outbox;

internal sealed record OutboxClaimedMessage(
    Guid Id,
    string EventType,
    string Payload,
    int RetryCount,
    Guid LockToken
);
