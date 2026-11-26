using System.Collections.Concurrent;
using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Infrastructure.Transports;

/// <summary>Thread-safe bounded ring buffer capturing messages sent via development in-memory and push stub transports.</summary>
public sealed class InMemoryCommunicationSink
{
    private const int MaxRetainedMessages = 500;
    private readonly ConcurrentQueue<InMemoryCapturedMessageResponse> _messages = new();

    /// <summary>Appends a captured outbound message to the in-memory sink.</summary>
    public void Record(
        OutboundTransportMessage message,
        string transportKey,
        string providerMessageId,
        DateTimeOffset capturedOnUtc
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Enqueue(
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

        while (_messages.Count > MaxRetainedMessages && _messages.TryDequeue(out _)) { }
    }

    /// <summary>Queries recent captured messages in reverse chronological order.</summary>
    public IReadOnlyList<InMemoryCapturedMessageResponse> Query(
        CommunicationChannel? channel = null,
        string? recipientFilter = null,
        int limit = 50
    )
    {
        var boundedLimit = Math.Clamp(limit, 1, 200);
        var snapshot = _messages.Reverse();

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

    /// <summary>Clears all retained messages (useful in isolated test scenarios).</summary>
    public void Clear()
    {
        while (_messages.TryDequeue(out _)) { }
    }
}
