using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Scoped <see cref="SaveChangesInterceptor" /> that stamps <see cref="IAuditableEntity" /> timestamps,
/// rotates <see cref="IHasConcurrencyToken" /> stamps, captures integration events into the transactional Outbox,
/// and dispatches <see cref="IDomainEvent" />s only after commit succeeds.
/// </summary>
public sealed class AuditAndConcurrencySaveChangesInterceptor(
    IClock clock,
    IDomainEventDispatcher domainEventDispatcher,
    ILogger<AuditAndConcurrencySaveChangesInterceptor> logger
) : SaveChangesInterceptor
{
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher
        ?? throw new ArgumentNullException(nameof(domainEventDispatcher));

    private readonly ILogger<AuditAndConcurrencySaveChangesInterceptor> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    private readonly ConditionalWeakTable<DbContext, StagedSaveChangesState> _stagedStates = [];

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        PrepareChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        PrepareChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var events = DrainStagedEvents(eventData.Context);
        if (events.Count > 0)
        {
            DispatchEventsSynchronously(events);
        }

        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var events = DrainStagedEvents(eventData.Context);
        if (events.Count > 0)
        {
            await _domainEventDispatcher.DispatchAsync(events, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        DiscardStagedEvents(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        DiscardStagedEvents(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void PrepareChanges(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DiscardStagedEvents(context);
        if (context is ModuleDbContext moduleContext)
        {
            TenantWriteGuardStamper.EnforceAndStampTenant(moduleContext);
        }

        var utcNow = _clock.UtcNow;
        AuditAndConcurrencyStamper.StampAuditMetadata(context, utcNow);
        AuditAndConcurrencyStamper.UpdateConcurrencyTokens(context, utcNow);
        CaptureOutboxAndStageEvents(context);
    }

    private void CaptureOutboxAndStageEvents(DbContext context)
    {
        var state = new StagedSaveChangesState();
        _stagedStates.AddOrUpdate(context, state);

        try
        {
            var moduleContext = context as ModuleDbContext;
            if (moduleContext is not null)
            {
                OutboxEventStager.CaptureIntegrationEvents(moduleContext, state);
            }

            OutboxEventStager.StageDomainEvents(context, moduleContext, state);
            if (!state.HasAny)
            {
                _ = _stagedStates.Remove(context);
            }
        }
        catch
        {
            DiscardStagedEvents(context);
            throw;
        }
    }

    private List<IDomainEvent> DrainStagedEvents(DbContext? context)
    {
        if (context is null || !_stagedStates.TryGetValue(context, out var state))
        {
            return [];
        }

        _ = _stagedStates.Remove(context);

        foreach (var source in state.IntegrationEventSources)
        {
            source.ClearIntegrationEvents();
        }

        var events = new List<IDomainEvent>();
        foreach (var entry in state.DomainEvents)
        {
            entry.Aggregate.ClearDomainEvents();
            events.AddRange(entry.Events);
        }

        if (events.Count > 0)
        {
            EntityFrameworkLog.DrainedDomainEvents(_logger, events.Count, context.GetType().Name);
        }

        return events;
    }

    private void DiscardStagedEvents(DbContext? context)
    {
        if (context is null || !_stagedStates.TryGetValue(context, out var state))
        {
            return;
        }

        _ = _stagedStates.Remove(context);
        foreach (var outboxMessage in state.StagedOutboxMessages)
        {
            var entry = context.Entry(outboxMessage);
            if (entry.State == EntityState.Added)
            {
                entry.State = EntityState.Detached;
            }
        }

        EntityFrameworkLog.DiscardedDomainEvents(_logger, context.GetType().Name);
    }

    private void DispatchEventsSynchronously(List<IDomainEvent> events)
    {
        if (SynchronizationContext.Current is null)
        {
            _domainEventDispatcher.DispatchAsync(events).GetAwaiter().GetResult();
            return;
        }

        Task.Run(() => _domainEventDispatcher.DispatchAsync(events)).GetAwaiter().GetResult();
    }
}
