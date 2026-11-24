namespace Ruth.Persistence.Outbox;

internal sealed record OutboxClaimedMessage(
    Guid Id,
    string EventType,
    string Payload,
    DateTimeOffset OccurredOnUtc,
    int RetryCount,
    Guid LockToken
);
