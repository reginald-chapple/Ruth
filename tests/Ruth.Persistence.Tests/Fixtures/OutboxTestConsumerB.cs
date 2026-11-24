using Ruth.SharedKernel.Events;

namespace Ruth.Persistence.Tests.Fixtures;

internal sealed class OutboxTestConsumerB(OutboxTestConsumerState state)
    : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
{
    public Task ConsumeAsync(
        TestWidgetIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        var attempt = Interlocked.Increment(ref state.ConsumerBAttempts);
        if (state.FailConsumerBOnce && attempt == 1)
        {
            throw new TimeoutException("Transient downstream timeout.");
        }

        state.ConsumerBEvents.Add(integrationEvent.EventId);
        return Task.CompletedTask;
    }
}
