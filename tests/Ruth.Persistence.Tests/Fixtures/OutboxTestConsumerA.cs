using Ruth.SharedKernel.Events;

namespace Ruth.Persistence.Tests.Fixtures;

internal sealed class OutboxTestConsumerA(OutboxTestConsumerState state)
    : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
{
    public Task ConsumeAsync(
        TestWidgetIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        if (state.AlwaysFailConsumerA)
        {
            throw new InvalidOperationException("Permanent consumer failure.");
        }

        state.ConsumerAEvents.Add(integrationEvent.EventId);
        return Task.CompletedTask;
    }
}
