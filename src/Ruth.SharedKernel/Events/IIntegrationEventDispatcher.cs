namespace Ruth.SharedKernel.Events;

/// <summary>Dispatches integration events to all registered <see cref="IIntegrationEventConsumer{TEvent}" /> instances.</summary>
public interface IIntegrationEventDispatcher
{
    /// <summary>Dispatches a strongly-typed integration event to all registered consumers.</summary>
    /// <typeparam name="TEvent">The integration event type.</typeparam>
    /// <param name="integrationEvent">The integration event to dispatch.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task DispatchAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
        where TEvent : IIntegrationEvent;

    /// <summary>Dispatches a polymorphic integration event to its registered consumers.</summary>
    /// <param name="integrationEvent">The integration event to dispatch.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    );
}
