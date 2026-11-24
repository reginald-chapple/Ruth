namespace Ruth.SharedKernel.Events;

/// <summary>Consumes an in-process <see cref="IDomainEvent" /> within a bounded context.</summary>
/// <typeparam name="TEvent">The concrete domain event type.</typeparam>
public interface IDomainEventConsumer<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>Processes the specified domain event.</summary>
    /// <param name="domainEvent">The raised domain event.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task ConsumeAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
