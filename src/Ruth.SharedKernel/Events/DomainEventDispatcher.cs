using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ruth.SharedKernel.Events;

/// <summary>Explicit DI-backed dispatcher for <see cref="IDomainEvent" /> instances.</summary>
public sealed partial class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger
) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, IEventInvoker> _invokerCache = new();

    private readonly IServiceProvider _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ILogger<DomainEventDispatcher> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task DispatchAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default
    )
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var consumers = _serviceProvider.GetServices<IDomainEventConsumer<TEvent>>();
        LogDispatchingEvent(_logger, typeof(TEvent).Name, domainEvent.EventId);

        foreach (var consumer in consumers)
        {
            await consumer.ConsumeAsync(domainEvent, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is null)
            {
                throw new ArgumentException(
                    "Domain event collection cannot contain null entries.",
                    nameof(domainEvents)
                );
            }

            var eventType = domainEvent.GetType();
            var invoker = _invokerCache.GetOrAdd(eventType, static type => CreateInvoker(type));
            LogDispatchingEvent(_logger, eventType.Name, domainEvent.EventId);
            await invoker.InvokeAsync(_serviceProvider, domainEvent, cancellationToken);
        }
    }

    private static IEventInvoker CreateInvoker(Type eventType)
    {
        var invokerType = typeof(TypedEventInvoker<>).MakeGenericType(eventType);
        return (IEventInvoker)Activator.CreateInstance(invokerType)!;
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Dispatching domain event {EventType} ({DomainEventId})."
    )]
    private static partial void LogDispatchingEvent(
        ILogger logger,
        string eventType,
        Guid domainEventId
    );

    private interface IEventInvoker
    {
        Task InvokeAsync(
            IServiceProvider serviceProvider,
            IDomainEvent domainEvent,
            CancellationToken cancellationToken
        );
    }

    private sealed class TypedEventInvoker<TEvent> : IEventInvoker
        where TEvent : IDomainEvent
    {
        public async Task InvokeAsync(
            IServiceProvider serviceProvider,
            IDomainEvent domainEvent,
            CancellationToken cancellationToken
        )
        {
            var typedEvent = (TEvent)domainEvent;
            var consumers = serviceProvider.GetServices<IDomainEventConsumer<TEvent>>();

            foreach (var consumer in consumers)
            {
                await consumer.ConsumeAsync(typedEvent, cancellationToken);
            }
        }
    }
}
