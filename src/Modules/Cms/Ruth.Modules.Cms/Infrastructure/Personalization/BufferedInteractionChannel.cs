using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Cms.Contracts.Personalization;

namespace Ruth.Modules.Cms.Infrastructure.Personalization;

internal sealed partial class BufferedInteractionChannel(
    IServiceScopeFactory scopeFactory,
    ILogger<BufferedInteractionChannel> logger
) : IInteractionBuffer, IDisposable
{
    private const int ChannelCapacity = 16384;
    private const int MaxBatchSize = 512;

    private readonly Channel<TrackInteractionItemSpec> _channel =
        Channel.CreateBounded<TrackInteractionItemSpec>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
            }
        );

    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private int _pendingCount;
    private long _totalFlushedCount;

    public async ValueTask EnqueueAsync(
        TrackInteractionItemSpec item,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        await _channel.Writer.WriteAsync(item, cancellationToken);
        _ = Interlocked.Increment(ref _pendingCount);
    }

    public InteractionBufferStatusResponse GetStatus(int enqueuedInCall = 0)
    {
        return new InteractionBufferStatusResponse(
            enqueuedInCall,
            Math.Max(0, Volatile.Read(ref _pendingCount)),
            Interlocked.Read(ref _totalFlushedCount)
        );
    }

    public async Task<InteractionBufferStatusResponse> FlushAsync(
        CancellationToken cancellationToken = default
    )
    {
        await _flushLock.WaitAsync(cancellationToken);
        try
        {
            var flushedNow = 0;
            while (true)
            {
                var batch = DrainBatch();
                if (batch.Count == 0)
                {
                    break;
                }

                await using var scope = scopeFactory.CreateAsyncScope();
                var persister =
                    scope.ServiceProvider.GetRequiredService<InteractionBatchPersister>();
                await persister.PersistBatchAsync(batch, cancellationToken);

                flushedNow += batch.Count;
                _ = Interlocked.Add(ref _totalFlushedCount, batch.Count);
            }

            if (flushedNow > 0)
            {
                LogFlushedBatch(logger, flushedNow);
            }

            return GetStatus(flushedNow);
        }
        finally
        {
            _ = _flushLock.Release();
        }
    }

    public void Dispose()
    {
        _flushLock.Dispose();
    }

    private List<TrackInteractionItemSpec> DrainBatch()
    {
        var batch = new List<TrackInteractionItemSpec>(Math.Min(MaxBatchSize, 64));
        while (batch.Count < MaxBatchSize && _channel.Reader.TryRead(out var item))
        {
            _ = Interlocked.Decrement(ref _pendingCount);
            batch.Add(item);
        }

        return batch;
    }

    [LoggerMessage(
        EventId = 6301,
        Level = LogLevel.Debug,
        Message = "Flushed {Count} buffered CMS interaction(s) to PostgreSQL."
    )]
    private static partial void LogFlushedBatch(ILogger logger, int count);
}
