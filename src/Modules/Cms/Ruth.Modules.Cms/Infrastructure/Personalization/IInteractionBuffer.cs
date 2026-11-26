using Ruth.Modules.Cms.Contracts.Personalization;

namespace Ruth.Modules.Cms.Infrastructure.Personalization;

/// <summary>High-throughput non-blocking interaction ingestion buffer drained in batches into PostgreSQL.</summary>
public interface IInteractionBuffer
{
    /// <summary>Enqueues an interaction spec into the bounded ingestion channel.</summary>
    ValueTask EnqueueAsync(
        TrackInteractionItemSpec item,
        CancellationToken cancellationToken = default
    );

    /// <summary>Drains all currently buffered interactions and commits them in a batch.</summary>
    Task<InteractionBufferStatusResponse> FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets current buffer queue depth and total flushed interaction count.</summary>
    InteractionBufferStatusResponse GetStatus(int enqueuedInCall = 0);
}
