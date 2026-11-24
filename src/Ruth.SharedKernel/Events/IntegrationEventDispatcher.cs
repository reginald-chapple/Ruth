using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ruth.SharedKernel.Events;

/// <summary>Explicit DI-backed dispatcher for <see cref="IIntegrationEvent" /> instances.</summary>
public sealed partial class IntegrationEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<IntegrationEventDispatcher> logger
) : IIntegrationEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, IEventInvoker> _invokerCache = new();

    private readonly IServiceProvider _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ILogger<IntegrationEventDispatcher> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task DispatchAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var consumers = _serviceProvider.GetServices<IIntegrationEventConsumer<TEvent>>();
        LogDispatchingEvent(_logger, typeof(TEvent).Name, integrationEvent.EventId);

        foreach (var consumer in consumers)
        {
            await consumer.ConsumeAsync(integrationEvent, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();
        var invoker = _invokerCache.GetOrAdd(eventType, static type => CreateInvoker(type));
        LogDispatchingEvent(_logger, eventType.Name, integrationEvent.EventId);
        await invoker.InvokeAsync(_serviceProvider, integrationEvent, cancellationToken);
    }

    private static IEventInvoker CreateInvoker(Type eventType)
    {
        var invokerType = typeof(TypedEventInvoker<>).MakeGenericType(eventType);
        return (IEventInvoker)Activator.CreateInstance(invokerType)!;
    }

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Debug,
        Message = "Dispatching integration event {EventType} ({IntegrationEventId})."
    )]
    private static partial void LogDispatchingEvent(
        ILogger logger,
        string eventType,
        Guid integrationEventId
    );

    private interface IEventInvoker
    {
        Task InvokeAsync(
            IServiceProvider serviceProvider,
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken
        );
    }

    private sealed class TypedEventInvoker<TEvent> : IEventInvoker
        where TEvent : IIntegrationEvent
    {
        public async Task InvokeAsync(
            IServiceProvider serviceProvider,
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken
        )
        {
            var typedEvent = (TEvent)integrationEvent;
            var consumers = serviceProvider.GetServices<IIntegrationEventConsumer<TEvent>>();

            foreach (var consumer in consumers)
            {
                await consumer.ConsumeAsync(typedEvent, cancellationToken);
            }
        }
    }
}
