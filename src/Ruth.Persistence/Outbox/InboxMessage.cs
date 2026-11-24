namespace Ruth.Persistence.Outbox;

/// <summary>Idempotency record tracking processed (MessageId, ConsumerName) pairs per module schema.</summary>
public sealed class InboxMessage
{
    private InboxMessage() { }

    /// <summary>Gets the integration event identifier.</summary>
    public Guid MessageId { get; private set; }

    /// <summary>Gets the stable consumer identifier.</summary>
    public string ConsumerName { get; private set; } = string.Empty;

    /// <summary>Gets the UTC instant when the consumer processed the message.</summary>
    public DateTimeOffset ProcessedOnUtc { get; private set; }

    /// <summary>Creates a new <see cref="InboxMessage" /> record.</summary>
    public static InboxMessage Create(
        Guid messageId,
        string consumerName,
        DateTimeOffset processedOnUtc
    )
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Inbox message ID cannot be empty.", nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        return new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = consumerName,
            ProcessedOnUtc = processedOnUtc.ToUniversalTime(),
        };
    }
}
