using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Recurring;

/// <summary>Deduplication log entry preventing duplicate processing of payment gateway webhook deliveries.</summary>
public sealed class ProcessedGatewayWebhook : Entity<ProcessedGatewayWebhookId>
{
    private ProcessedGatewayWebhook() { }

    private ProcessedGatewayWebhook(
        ProcessedGatewayWebhookId id,
        FinancialGatewayId gatewayId,
        string webhookEventId,
        GatewayWebhookEventKind eventKind,
        DateTimeOffset processedOnUtc
    )
        : base(id)
    {
        FinancialGatewayId = gatewayId;
        WebhookEventId = webhookEventId;
        EventKind = eventKind;
        ProcessedOnUtc = processedOnUtc.ToUniversalTime();
    }

    public FinancialGatewayId FinancialGatewayId { get; private set; }
    public string WebhookEventId { get; private set; } = string.Empty;
    public GatewayWebhookEventKind EventKind { get; private set; }
    public DateTimeOffset ProcessedOnUtc { get; private set; }

    public static ProcessedGatewayWebhook Create(
        FinancialGatewayId gatewayId,
        string webhookEventId,
        GatewayWebhookEventKind eventKind,
        DateTimeOffset processedOnUtc
    )
    {
        return new ProcessedGatewayWebhook(
            ProcessedGatewayWebhookId.Create(),
            gatewayId,
            webhookEventId.Trim(),
            eventKind,
            processedOnUtc
        );
    }
}
