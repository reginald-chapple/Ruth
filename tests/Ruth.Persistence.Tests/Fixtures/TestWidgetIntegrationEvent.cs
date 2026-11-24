using Ruth.SharedKernel.Events;

namespace Ruth.Persistence.Tests.Fixtures;

public sealed record TestWidgetIntegrationEvent(Guid WidgetId, string Action) : IntegrationEvent;
