using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Outbox;

/// <summary>
/// Claims and dispatches transactional Outbox messages for <typeparamref name="TContext" />.
/// </summary>
/// <typeparam name="TContext">The module <see cref="ModuleDbContext" /> type.</typeparam>
public sealed class OutboxModuleProcessor<TContext> : IOutboxModuleProcessor
    where TContext : ModuleDbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxEventTypeRegistry _eventTypeRegistry;
    private readonly IClock _clock;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxModuleProcessor<TContext>> _logger;
    private readonly OutboxStatusUpdater<TContext> _statusUpdater;

    /// <summary>Initializes a new <see cref="OutboxModuleProcessor{TContext}" />.</summary>
    public OutboxModuleProcessor(
        IServiceScopeFactory scopeFactory,
        OutboxEventTypeRegistry eventTypeRegistry,
        IClock clock,
        IOptions<OutboxOptions> options,
        ILogger<OutboxModuleProcessor<TContext>> logger,
        OutboxSchemaDescriptor<TContext> schemaDescriptor
    )
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(eventTypeRegistry);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(schemaDescriptor);

        _scopeFactory = scopeFactory;
        _eventTypeRegistry = eventTypeRegistry;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
        Schema = SchemaValidator.Validate(schemaDescriptor.Schema);
        _statusUpdater = new OutboxStatusUpdater<TContext>(
            scopeFactory,
            clock,
            _options,
            logger,
            Schema
        );
    }

    /// <inheritdoc />
    public string Schema { get; }

    /// <inheritdoc />
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OutboxClaimedMessage> claimed;
        await using (var claimScope = _scopeFactory.CreateAsyncScope())
        {
            var dbContext = claimScope.ServiceProvider.GetRequiredService<TContext>();
            var nowUtc = _clock.UtcNow;
            claimed = await OutboxBatchClaimer.ClaimBatchAsync(
                dbContext,
                Schema,
                _options.BatchSize,
                nowUtc,
                nowUtc + _options.LockDuration,
                cancellationToken
            );
        }

        if (claimed.Count == 0)
        {
            return 0;
        }

        OutboxLog.ClaimedBatch(_logger, claimed.Count, Schema);
        foreach (var message in claimed)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }

        return claimed.Count;
    }

    /// <inheritdoc />
    public async Task<int> PruneProcessedAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default
    )
    {
        var utcCutoff = cutoffUtc.ToUniversalTime();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var deleted = await dbContext
            .OutboxMessages.Where(x => x.ProcessedOnUtc != null && x.ProcessedOnUtc < utcCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            OutboxLog.PrunedProcessed(_logger, deleted, Schema, utcCutoff);
        }

        return deleted;
    }

    private async Task ProcessMessageAsync(
        OutboxClaimedMessage message,
        CancellationToken cancellationToken
    )
    {
        bool deserialized;
        IIntegrationEvent? integrationEvent;
        string? errorReason;

        try
        {
            deserialized = TryDeserializeEvent(message, out integrationEvent, out errorReason);
        }
        catch (Exception ex)
        {
            await _statusUpdater.DeadLetterAsync(
                message.Id,
                message.LockToken,
                message.RetryCount,
                $"Deserialization failed: {ex.GetType().Name}: {ex.Message}",
                ex,
                cancellationToken
            );
            return;
        }

        if (!deserialized)
        {
            await _statusUpdater.DeadLetterAsync(
                message.Id,
                message.LockToken,
                message.RetryCount,
                errorReason!,
                exception: null,
                cancellationToken
            );
            return;
        }

        try
        {
            await using var messageScope = _scopeFactory.CreateAsyncScope();
            var dispatcher =
                messageScope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
            await dispatcher.DispatchAsync(integrationEvent!, cancellationToken);
            await _statusUpdater.MarkProcessedAsync(
                message.Id,
                message.LockToken,
                cancellationToken
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _statusUpdater.HandleFailureAsync(message, ex, cancellationToken);
        }
    }

    private bool TryDeserializeEvent(
        OutboxClaimedMessage message,
        out IIntegrationEvent? integrationEvent,
        out string? errorReason
    )
    {
        var eventType = _eventTypeRegistry.Resolve(message.EventType);
        if (eventType is null)
        {
            integrationEvent = null;
            errorReason = $"Unknown integration event type '{message.EventType}'.";
            return false;
        }

        try
        {
            integrationEvent = OutboxJsonSerializer.Deserialize(message.Payload, eventType);
            if (integrationEvent is null)
            {
                errorReason = $"Deserialized payload was null for '{message.EventType}'.";
                return false;
            }

            errorReason = null;
            return true;
        }
        catch (JsonException ex)
        {
            integrationEvent = null;
            errorReason = OutboxStatusUpdater<TContext>.TruncateError(
                $"JSON deserialization failed: {ex.Message}"
            );
            return false;
        }
    }
}
