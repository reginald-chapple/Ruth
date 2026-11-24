namespace Ruth.SharedKernel.Events;

/// <summary>Dispatches domain events to all registered <see cref="IDomainEventConsumer{TEvent}" /> instances.</summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches a strongly-typed domain event to all registered consumers.</summary>
    /// <typeparam name="TEvent">The domain event type.</typeparam>
    /// <param name="domainEvent">The domain event to dispatch.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>Dispatches a sequence of domain events in order to their registered consumers.</summary>
    /// <param name="domainEvents">The domain events to dispatch.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default
    );
}
