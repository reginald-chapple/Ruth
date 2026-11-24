using System.Text.Json;
using Ruth.SharedKernel.Events;

namespace Ruth.Persistence.Outbox;

internal static class OutboxJsonSerializer
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static string Serialize(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), _options);
    }

    public static IIntegrationEvent? Deserialize(string payload, Type eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentNullException.ThrowIfNull(eventType);

        return JsonSerializer.Deserialize(payload, eventType, _options) as IIntegrationEvent;
    }
}
