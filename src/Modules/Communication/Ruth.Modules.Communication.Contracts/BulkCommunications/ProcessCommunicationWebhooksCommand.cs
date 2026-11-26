using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Processes a batch of provider bounce, open, click, complaint, and unsubscribe webhook events.</summary>
/// <param name="Events">Batch of provider telemetry events (1..500).</param>
public sealed record ProcessCommunicationWebhooksCommand(
    IReadOnlyList<CommunicationWebhookEventDto> Events
) : ICommand<CommunicationWebhookProcessResponse>;
