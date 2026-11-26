using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.Outbox;

/// <summary>Idempotency record tracking processed (MessageId, ConsumerName) pairs per module schema.</summary>
public sealed class InboxMessage : ITenantScoped
{
    private InboxMessage() { }

    /// <inheritdoc />
    public TenantId TenantId { get; private set; }

    /// <summary>Gets the integration event identifier.</summary>
    public Guid MessageId { get; private set; }

    /// <summary>Gets the stable consumer identifier.</summary>
    public string ConsumerName { get; private set; } = string.Empty;

    /// <summary>Gets the UTC instant when the consumer processed the message.</summary>
    public DateTimeOffset ProcessedOnUtc { get; private set; }

    /// <inheritdoc />
    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign InboxMessage ({MessageId}) from tenant '{TenantId}' to '{tenantId}'."
            );
        }

        TenantId = tenantId;
    }

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
