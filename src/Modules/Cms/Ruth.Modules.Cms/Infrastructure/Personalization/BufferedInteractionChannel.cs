using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Cms.Infrastructure.Personalization;

internal sealed partial class BufferedInteractionChannel(
    IServiceScopeFactory scopeFactory,
    ILogger<BufferedInteractionChannel> logger
) : IInteractionBuffer, IDisposable
{
    private const int ChannelCapacity = 16384;
    private const int MaxBatchSize = 512;

    private sealed class TenantInteractionCounters
    {
        public int PendingCount;
        public long FlushedCount;
    }

    private readonly record struct BufferedInteractionEnvelope(
        TenantExecutionState TenantState,
        TrackInteractionItemSpec Spec
    );

    private readonly Channel<BufferedInteractionEnvelope> _channel =
        Channel.CreateBounded<BufferedInteractionEnvelope>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
            }
        );

    private readonly ConcurrentDictionary<TenantId, TenantInteractionCounters> _tenantCounters =
        new();

    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private int _totalPendingCount;
    private long _totalFlushedCount;

    public async ValueTask EnqueueAsync(
        TrackInteractionItemSpec item,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        var state = AmbientTenantContext.CurrentState;
        if (state.TenantId.IsEmpty)
        {
            throw new TenantContextRequiredException(
                "CMS interaction buffering requires an active tenant context."
            );
        }

        await _channel.Writer.WriteAsync(
            new BufferedInteractionEnvelope(state, item),
            cancellationToken
        );
        var counters = _tenantCounters.GetOrAdd(
            state.TenantId,
            static _ => new TenantInteractionCounters()
        );
        _ = Interlocked.Increment(ref counters.PendingCount);
        _ = Interlocked.Increment(ref _totalPendingCount);
    }

    public InteractionBufferStatusResponse GetStatus(int enqueuedInCall = 0)
    {
        var activeTenantId = AmbientTenantContext.CurrentTenantId;
        return activeTenantId.IsEmpty
                ? new InteractionBufferStatusResponse(
                    enqueuedInCall,
                    Math.Max(0, Volatile.Read(ref _totalPendingCount)),
                    Interlocked.Read(ref _totalFlushedCount)
                )
            : _tenantCounters.TryGetValue(activeTenantId, out var counters)
                ? new InteractionBufferStatusResponse(
                    enqueuedInCall,
                    Math.Max(0, Volatile.Read(ref counters.PendingCount)),
                    Interlocked.Read(ref counters.FlushedCount)
                )
            : new InteractionBufferStatusResponse(enqueuedInCall, 0, 0);
    }

    public async Task<InteractionBufferStatusResponse> FlushAsync(
        CancellationToken cancellationToken = default
    )
    {
        var callerTenantId = AmbientTenantContext.CurrentTenantId;
        await _flushLock.WaitAsync(cancellationToken);
        try
        {
            var flushedTotalNow = 0;
            var flushedCallerNow = 0;
            while (true)
            {
                var batch = DrainBatch();
                if (batch.Count == 0)
                {
                    break;
                }

                var callerDelta = await PersistGroupedByTenantAsync(
                    batch,
                    callerTenantId,
                    cancellationToken
                );
                flushedTotalNow += batch.Count;
                flushedCallerNow += callerDelta;
            }

            if (flushedTotalNow > 0)
            {
                LogFlushedBatch(logger, flushedTotalNow);
            }

            return GetStatus(callerTenantId.IsEmpty ? flushedTotalNow : flushedCallerNow);
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

    private async Task<int> PersistGroupedByTenantAsync(
        List<BufferedInteractionEnvelope> batch,
        TenantId callerTenantId,
        CancellationToken cancellationToken
    )
    {
        var callerFlushed = 0;
        foreach (var group in batch.GroupBy(static e => e.TenantState.TenantId))
        {
            var state = group.First().TenantState;
            TrackInteractionItemSpec[] specs = [.. group.Select(static e => e.Spec)];
            using (
                AmbientTenantContext.BeginScope(
                    state.TenantId,
                    state.TenantSlug,
                    state.LifecycleStage
                )
            )
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var persister =
                    scope.ServiceProvider.GetRequiredService<InteractionBatchPersister>();
                await persister.PersistBatchAsync(specs, cancellationToken);
            }

            var counters = _tenantCounters.GetOrAdd(
                state.TenantId,
                static _ => new TenantInteractionCounters()
            );
            _ = Interlocked.Add(ref counters.FlushedCount, specs.Length);
            _ = Interlocked.Add(ref _totalFlushedCount, specs.Length);
            if (!callerTenantId.IsEmpty && state.TenantId == callerTenantId)
            {
                callerFlushed += specs.Length;
            }
        }

        return callerFlushed;
    }

    private List<BufferedInteractionEnvelope> DrainBatch()
    {
        var batch = new List<BufferedInteractionEnvelope>(Math.Min(MaxBatchSize, 64));
        while (batch.Count < MaxBatchSize && _channel.Reader.TryRead(out var envelope))
        {
            _ = Interlocked.Decrement(ref _totalPendingCount);
            if (_tenantCounters.TryGetValue(envelope.TenantState.TenantId, out var counters))
            {
                _ = Interlocked.Decrement(ref counters.PendingCount);
            }

            batch.Add(envelope);
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
