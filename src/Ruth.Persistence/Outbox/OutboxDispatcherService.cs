using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Outbox;

/// <summary>
/// Hosted background service polling all registered module Outbox processors using <see cref="PeriodicTimer" />.
/// </summary>
public sealed class OutboxDispatcherService(
    IEnumerable<IOutboxModuleProcessor> processors,
    IOptions<OutboxOptions> options,
    IClock clock,
    ILogger<OutboxDispatcherService> logger
) : BackgroundService
{
    private readonly IOutboxModuleProcessor[] _processors = processors?.ToArray()
        ?? throw new ArgumentNullException(nameof(processors));

    private readonly OutboxOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    private readonly ILogger<OutboxDispatcherService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Runs one sweep across all registered module Outbox processors.</summary>
    public async Task<int> ProcessPendingOnceAsync(CancellationToken cancellationToken = default)
    {
        var processedCount = 0;
        foreach (var processor in _processors)
        {
            try
            {
                processedCount += await processor.ProcessBatchAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                OutboxLog.SchemaBatchFailed(_logger, ex, processor.Schema);
            }
        }

        return processedCount;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_processors.Length == 0)
        {
            return;
        }

        using var timer = new PeriodicTimer(_options.PollingInterval, _clock.TimeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _ = await ProcessPendingOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                OutboxLog.SweepFailed(_logger, ex);
            }
        }
    }
}
