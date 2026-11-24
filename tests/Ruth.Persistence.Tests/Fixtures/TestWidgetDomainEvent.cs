using Ruth.SharedKernel.Events;

namespace Ruth.Persistence.Tests.Fixtures;

public sealed record TestWidgetDomainEvent(TestWidgetId WidgetId, string Name) : DomainEvent;
