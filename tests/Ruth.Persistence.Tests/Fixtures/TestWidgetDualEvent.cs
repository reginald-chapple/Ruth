using Ruth.SharedKernel.Events;

namespace Ruth.Persistence.Tests.Fixtures;

public sealed record TestWidgetDualEvent(TestWidgetId WidgetId, string Name)
    : DomainEvent,
        IIntegrationEvent;
