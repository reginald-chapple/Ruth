using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.Outbox;

/// <summary>
/// Claims and dispatches transactional Outbox messages for <typeparamref name="TContext" />,
/// iterating active tenants in fair-share round-robin order under isolated <see cref="AmbientTenantContext" /> scopes.
/// </summary>
/// <typeparam name="TContext">The module <see cref="ModuleDbContext" /> type.</typeparam>
public sealed class OutboxModuleProcessor<TContext> : IOutboxModuleProcessor
    where TContext : ModuleDbContext
{
    private const int FairShareMultiTenantCap = 25;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxEventTypeRegistry _eventTypeRegistry;
    private readonly IClock _clock;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxModuleProcessor<TContext>> _logger;
    private readonly OutboxStatusUpdater<TContext> _statusUpdater;
    private readonly IActiveTenantRegistry? _activeTenantRegistry;
    private readonly bool _isTenantScoped;
    private int _roundRobinCursor;

    /// <summary>Initializes a new <see cref="OutboxModuleProcessor{TContext}" />.</summary>
    public OutboxModuleProcessor(
        IServiceScopeFactory scopeFactory,
        OutboxEventTypeRegistry eventTypeRegistry,
        IClock clock,
        IOptions<OutboxOptions> options,
        ILogger<OutboxModuleProcessor<TContext>> logger,
        OutboxSchemaDescriptor<TContext> schemaDescriptor,
        IActiveTenantRegistry? activeTenantRegistry = null
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
        _activeTenantRegistry = activeTenantRegistry;
        _isTenantScoped = schemaDescriptor.IsTenantScoped;
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
        if (!_isTenantScoped)
        {
            return await ProcessSingleScopeBatchAsync(_options.BatchSize, cancellationToken);
        }

        var tenants = await ResolveTargetTenantsAsync(cancellationToken);
        if (tenants.Count == 0)
        {
            return 0;
        }

        var perTenantQuantum =
            tenants.Count > 1
                ? Math.Max(1, Math.Min(_options.BatchSize, FairShareMultiTenantCap))
                : _options.BatchSize;
        var startOffset = ComputeStartOffset(tenants.Count);
        var totalProcessed = 0;

        for (var i = 0; i < tenants.Count; i++)
        {
            var tenant = tenants[(startOffset + i) % tenants.Count];
            totalProcessed += await ProcessTenantSafelyAsync(
                tenant,
                perTenantQuantum,
                cancellationToken
            );
        }

        return totalProcessed;
    }

    /// <inheritdoc />
    public async Task<int> PruneProcessedAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default
    )
    {
        var utcCutoff = cutoffUtc.ToUniversalTime();
        if (!_isTenantScoped)
        {
            return await PruneSingleScopeAsync(utcCutoff, cancellationToken);
        }

        var tenants = await ResolveTargetTenantsAsync(cancellationToken);
        var totalDeleted = 0;
        foreach (var tenant in tenants)
        {
            totalDeleted += await PruneTenantSafelyAsync(tenant, utcCutoff, cancellationToken);
        }

        return totalDeleted;
    }

    private int ComputeStartOffset(int count)
    {
        var next = unchecked((uint)Interlocked.Increment(ref _roundRobinCursor));
        return (int)(next % (uint)count);
    }

    private async Task<int> ProcessTenantSafelyAsync(
        TenantExecutionState tenant,
        int batchQuantum,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using (
                AmbientTenantContext.BeginScope(
                    tenant.TenantId,
                    tenant.TenantSlug,
                    tenant.LifecycleStage
                )
            )
            {
                return await ProcessSingleScopeBatchAsync(batchQuantum, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            OutboxLog.TenantBatchFailed(_logger, ex, tenant.TenantId.Value, Schema);
            return 0;
        }
    }

    private async Task<int> PruneTenantSafelyAsync(
        TenantExecutionState tenant,
        DateTimeOffset utcCutoff,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using (
                AmbientTenantContext.BeginScope(
                    tenant.TenantId,
                    tenant.TenantSlug,
                    tenant.LifecycleStage
                )
            )
            {
                return await PruneSingleScopeAsync(utcCutoff, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            OutboxLog.TenantBatchFailed(_logger, ex, tenant.TenantId.Value, Schema);
            return 0;
        }
    }

    private async ValueTask<IReadOnlyList<TenantExecutionState>> ResolveTargetTenantsAsync(
        CancellationToken cancellationToken
    )
    {
        if (_activeTenantRegistry is not null)
        {
            return await _activeTenantRegistry.GetActiveTenantsAsync(cancellationToken);
        }

        var current = AmbientTenantContext.CurrentState;
        return current.TenantId.IsEmpty ? [] : [current];
    }

    private async Task<int> ProcessSingleScopeBatchAsync(
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<OutboxClaimedMessage> claimed;
        await using (var claimScope = _scopeFactory.CreateAsyncScope())
        {
            var dbContext = claimScope.ServiceProvider.GetRequiredService<TContext>();
            var nowUtc = _clock.UtcNow;
            claimed = await OutboxBatchClaimer.ClaimBatchAsync(
                dbContext,
                Schema,
                batchSize,
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

    private async Task<int> PruneSingleScopeAsync(
        DateTimeOffset utcCutoff,
        CancellationToken cancellationToken
    )
    {
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
        var integrationEvent = await TryDeserializeOrDeadLetterAsync(message, cancellationToken);
        if (integrationEvent is null)
        {
            return;
        }

        try
        {
            await using var messageScope = _scopeFactory.CreateAsyncScope();
            var dispatcher =
                messageScope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
            await dispatcher.DispatchAsync(integrationEvent, cancellationToken);
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

    private async Task<IIntegrationEvent?> TryDeserializeOrDeadLetterAsync(
        OutboxClaimedMessage message,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (TryDeserializeEvent(message, out var integrationEvent, out var errorReason))
            {
                return integrationEvent;
            }

            await _statusUpdater.DeadLetterAsync(
                message.Id,
                message.LockToken,
                message.RetryCount,
                errorReason!,
                exception: null,
                cancellationToken
            );
            return null;
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
            return null;
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
