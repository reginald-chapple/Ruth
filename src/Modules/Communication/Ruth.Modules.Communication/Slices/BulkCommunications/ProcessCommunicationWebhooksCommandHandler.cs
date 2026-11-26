using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class ProcessCommunicationWebhooksCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock
) : ICommandHandler<ProcessCommunicationWebhooksCommand, CommunicationWebhookProcessResponse>
{
    public async Task<Result<CommunicationWebhookProcessResponse>> HandleAsync(
        ProcessCommunicationWebhooksCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Events is null || command.Events.Count == 0)
        {
            return Result.Failure<CommunicationWebhookProcessResponse>(
                BulkCommunicationErrors.WebhookEventsEmpty
            );
        }

        int processed = 0,
            unmatched = 0,
            suppressions = 0;
        var defaultNow = clock.UtcNow.ToUniversalTime();

        foreach (var evt in command.Events.Take(500))
        {
            var recipient = await ResolveRecipientAsync(evt, cancellationToken);
            if (recipient is null)
            {
                unmatched++;
                continue;
            }

            var comm = await dbContext
                .Communications.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == recipient.CommunicationId, cancellationToken);
            var channel = comm?.Channel ?? CommunicationChannel.Email;
            var topicKey = comm?.TopicKey ?? "general-announcements";
            var occurredAt = (evt.OccurredAtUtc ?? defaultNow).ToUniversalTime();

            suppressions += await ApplyWebhookEventAsync(
                recipient,
                channel,
                topicKey,
                evt,
                occurredAt,
                cancellationToken
            );
            processed++;
        }

        if (processed > 0 || suppressions > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new CommunicationWebhookProcessResponse(processed, unmatched, suppressions);
    }

    private async Task<CommunicationRecipient?> ResolveRecipientAsync(
        CommunicationWebhookEventDto evt,
        CancellationToken cancellationToken
    )
    {
        if (evt.RecipientId.HasValue && evt.RecipientId.Value != Guid.Empty)
        {
            var rid = CommunicationRecipientId.From(evt.RecipientId.Value);
            return await dbContext.CommunicationRecipients.FirstOrDefaultAsync(
                r => r.Id == rid,
                cancellationToken
            );
        }

        if (!string.IsNullOrWhiteSpace(evt.ProviderMessageId))
        {
            var msgId = evt.ProviderMessageId.Trim();
            return await dbContext.CommunicationRecipients.FirstOrDefaultAsync(
                r => r.ProviderMessageId == msgId,
                cancellationToken
            );
        }

        if (!string.IsNullOrWhiteSpace(evt.DestinationAddress))
        {
            var norm = evt.DestinationAddress.Trim().ToLowerInvariant();
            return await dbContext
                .CommunicationRecipients.OrderByDescending(static r => r.CreatedOnUtc)
                .FirstOrDefaultAsync(r => r.NormalizedDestination == norm, cancellationToken);
        }

        return null;
    }

    private async Task<int> ApplyWebhookEventAsync(
        CommunicationRecipient recipient,
        CommunicationChannel channel,
        string topicKey,
        CommunicationWebhookEventDto evt,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken
    )
    {
        switch (evt.EventType)
        {
            case CommunicationWebhookEventType.Delivered:
                recipient.MarkDelivered(
                    recipient.TransportKey ?? "webhook",
                    evt.ProviderMessageId ?? recipient.ProviderMessageId,
                    occurredAt
                );
                return 0;
            case CommunicationWebhookEventType.Opened:
                recipient.RecordOpen(occurredAt);
                return 0;
            case CommunicationWebhookEventType.Clicked:
                recipient.RecordClick(evt.ClickedUrl, occurredAt);
                return 0;
            case CommunicationWebhookEventType.Bounced:
                recipient.RecordBounce(evt.BounceKind, evt.Reason);
                return evt.BounceKind == BounceKind.Hard
                    ? await RecordSuppressionAsync(
                        recipient,
                        channel,
                        "*",
                        "HardBounce",
                        cancellationToken
                    )
                    : 0;
            case CommunicationWebhookEventType.SpamComplaint:
                recipient.RecordBounce(BounceKind.SpamComplaint, evt.Reason ?? "Spam complaint");
                return await RecordSuppressionAsync(
                    recipient,
                    channel,
                    "*",
                    "SpamComplaint",
                    cancellationToken
                );
            case CommunicationWebhookEventType.Unsubscribed:
                return await RecordSuppressionAsync(
                    recipient,
                    channel,
                    topicKey,
                    "WebhookUnsubscribe",
                    cancellationToken
                );
            default:
                return 0;
        }
    }

    private async Task<int> RecordSuppressionAsync(
        CommunicationRecipient recipient,
        CommunicationChannel channel,
        string topicOrGlobal,
        string reason,
        CancellationToken cancellationToken
    )
    {
        var pref = dbContext.CommunicationSubscriptionPreferences.Local.FirstOrDefault(p =>
            p.Channel == channel
            && string.Equals(
                p.NormalizedDestination,
                recipient.NormalizedDestination,
                StringComparison.OrdinalIgnoreCase
            )
        )
            ?? await dbContext.CommunicationSubscriptionPreferences.FirstOrDefaultAsync(
                p =>
                    p.Channel == channel
                    && p.NormalizedDestination == recipient.NormalizedDestination,
                cancellationToken
            );

        if (pref is null)
        {
            var created = CommunicationSubscriptionPreference.Create(
                channel,
                recipient.DestinationAddress,
                recipient.PersonId
            );
            if (created.IsFailure)
            {
                return 0;
            }

            pref = created.Value;
            _ = dbContext.CommunicationSubscriptionPreferences.Add(pref);
        }

        pref.ApplyOneClickOptOut(topicOrGlobal, reason);
        return 1;
    }
}
