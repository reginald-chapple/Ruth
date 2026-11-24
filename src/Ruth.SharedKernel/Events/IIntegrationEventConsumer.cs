namespace Ruth.SharedKernel.Events;

/// <summary>Consumes an <see cref="IIntegrationEvent" /> published by another bounded context.</summary>
/// <typeparam name="TEvent">The concrete integration event type.</typeparam>
public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>Processes the specified integration event.</summary>
    /// <param name="integrationEvent">The received integration event.</param>
    /// <param name="cancellationToken">A token to observe while awaiting completion.</param>
    Task ConsumeAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
