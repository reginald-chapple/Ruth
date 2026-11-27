using System.Collections.Concurrent;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication.Infrastructure.Transports;

/// <summary>Thread-safe tenant-partitioned bounded ring buffer capturing messages sent via development in-memory and push stub transports.</summary>
public sealed class InMemoryCommunicationSink
{
    private const int MaxRetainedMessages = 500;
    private readonly ConcurrentDictionary<
        TenantId,
        ConcurrentQueue<InMemoryCapturedMessageResponse>
    > _messagesByTenant = [];

    /// <summary>Appends a captured outbound message to the active tenant's in-memory sink.</summary>
    public void Record(
        OutboundTransportMessage message,
        string transportKey,
        string providerMessageId,
        DateTimeOffset capturedOnUtc
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        var tenantId = AmbientTenantContext.CurrentTenantId;
        var queue = _messagesByTenant.GetOrAdd(
            tenantId,
            static _ => new ConcurrentQueue<InMemoryCapturedMessageResponse>()
        );

        queue.Enqueue(
            new InMemoryCapturedMessageResponse(
                message.MessageId,
                transportKey,
                message.Channel,
                message.ToAddress,
                message.FromAddress,
                message.Subject,
                message.HtmlBody,
                message.PlainTextBody,
                message.PushTitle,
                message.SystemCommunicationKey,
                providerMessageId,
                capturedOnUtc.ToUniversalTime()
            )
        );

        while (queue.Count > MaxRetainedMessages && queue.TryDequeue(out _)) { }
    }

    /// <summary>Queries recent captured messages for the active tenant in reverse chronological order.</summary>
    public IReadOnlyList<InMemoryCapturedMessageResponse> Query(
        CommunicationChannel? channel = null,
        string? recipientFilter = null,
        int limit = 50
    )
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (!_messagesByTenant.TryGetValue(tenantId, out var queue))
        {
            return [];
        }

        var boundedLimit = Math.Clamp(limit, 1, 200);
        var snapshot = queue.Reverse();

        if (channel.HasValue)
        {
            snapshot = snapshot.Where(m => m.Channel == channel.Value);
        }

        if (!string.IsNullOrWhiteSpace(recipientFilter))
        {
            var term = recipientFilter.Trim();
            snapshot = snapshot.Where(m =>
                m.ToAddress.Contains(term, StringComparison.OrdinalIgnoreCase)
            );
        }

        return [.. snapshot.Take(boundedLimit)];
    }

    /// <summary>Clears retained messages for the active tenant, or all partitions when no tenant is scoped.</summary>
    public void Clear()
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
        {
            _messagesByTenant.Clear();
            return;
        }

        _ = _messagesByTenant.TryRemove(tenantId, out _);
    }
}
