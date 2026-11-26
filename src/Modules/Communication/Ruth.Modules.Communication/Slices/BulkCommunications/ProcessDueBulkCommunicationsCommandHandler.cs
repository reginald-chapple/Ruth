using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class ProcessDueBulkCommunicationsCommandHandler(
    CommunicationDbContext dbContext,
    BulkEmailComposerService composerService,
    CommunicationTransportResolver transportResolver,
    IOptions<CommunicationTransportOptions> transportOptions,
    ITenantContext tenantContext,
    IClock clock
) : ICommandHandler<ProcessDueBulkCommunicationsCommand, BulkDispatchBatchResultResponse>
{
    private readonly CommunicationTransportOptions _transportOptions = transportOptions.Value;

    public async Task<Result<BulkDispatchBatchResultResponse>> HandleAsync(
        ProcessDueBulkCommunicationsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!tenantContext.IsLive)
        {
            return DomainError.Validation(
                "Communication.TenantNotLive",
                "Outbound bulk email and SMS dispatch requires IRS tax-exempt verification and Live workspace approval."
            );
        }

        var nowUtc = clock.UtcNow.ToUniversalTime();
        var remainingBudget = Math.Clamp(command.MaxRecipientsPerRun, 1, 1000);
        var dueComms = await LoadDueCommunicationsAsync(
            command.SpecificCommunicationId,
            nowUtc,
            cancellationToken
        );

        int touched = 0,
            completed = 0,
            delivered = 0,
            skipped = 0,
            failed = 0;
        foreach (var comm in dueComms)
        {
            if (remainingBudget <= 0)
            {
                break;
            }

            var (batchDelivered, batchSkipped, batchFailed, batchCompleted) =
                await DispatchSingleCommunicationBatchAsync(
                    comm,
                    remainingBudget,
                    nowUtc,
                    cancellationToken
                );
            touched++;
            delivered += batchDelivered;
            skipped += batchSkipped;
            failed += batchFailed;
            remainingBudget -= batchDelivered + batchSkipped + batchFailed;
            if (batchCompleted)
            {
                completed++;
            }
        }

        if (touched > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new BulkDispatchBatchResultResponse(touched, completed, delivered, skipped, failed);
    }

    private Task<List<Domain.BulkCommunications.Communication>> LoadDueCommunicationsAsync(
        Guid? specificId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var q = dbContext.Communications.Where(c =>
            (c.Status == CommunicationStatus.Approved || c.Status == CommunicationStatus.Sending)
            && (!c.ScheduledSendAtUtc.HasValue || c.ScheduledSendAtUtc <= nowUtc)
        );
        if (specificId.HasValue && specificId.Value != Guid.Empty)
        {
            var cid = CommunicationId.From(specificId.Value);
            q = q.Where(c => c.Id == cid);
        }

        return q.OrderBy(static c => c.CreatedOnUtc).Take(20).ToListAsync(cancellationToken);
    }

    private async Task<(
        int Delivered,
        int Skipped,
        int Failed,
        bool Completed
    )> DispatchSingleCommunicationBatchAsync(
        Domain.BulkCommunications.Communication comm,
        int budget,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        _ = comm.BeginSending();
        var pending = await dbContext
            .CommunicationRecipients.Where(r =>
                r.CommunicationId == comm.Id && r.Status == RecipientDeliveryStatus.Pending
            )
            .OrderBy(static r => r.Id)
            .Take(budget)
            .ToListAsync(cancellationToken);

        var prefsByDest = await LoadPreferencesMapAsync(comm.Channel, pending, cancellationToken);
        var senderPhone = await ResolveSenderPhoneAsync(
            comm.SenderSystemPhoneNumberId,
            cancellationToken
        );
        var transport = transportResolver.ResolveForChannel(comm.Channel);

        var (delivered, skipped, failed) = await ProcessRecipientChunkAsync(
            comm,
            pending,
            prefsByDest,
            senderPhone,
            transport,
            nowUtc,
            cancellationToken
        );

        var processedIds = pending.Select(static p => p.Id).ToArray();
        var hasMorePending = await dbContext.CommunicationRecipients.AnyAsync(
            r =>
                r.CommunicationId == comm.Id
                && r.Status == RecipientDeliveryStatus.Pending
                && !processedIds.Contains(r.Id),
            cancellationToken
        );

        if (!hasMorePending)
        {
            _ = comm.CompleteSending(nowUtc);
            await EnqueueCompletionEventAsync(
                comm,
                delivered,
                skipped,
                failed,
                nowUtc,
                cancellationToken
            );
            return (delivered, skipped, failed, true);
        }

        return (delivered, skipped, failed, false);
    }

    private async Task<(int Delivered, int Skipped, int Failed)> ProcessRecipientChunkAsync(
        Domain.BulkCommunications.Communication comm,
        List<CommunicationRecipient> pending,
        Dictionary<string, CommunicationSubscriptionPreference> prefsByDest,
        string? senderPhone,
        ICommunicationTransport transport,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        int delivered = 0,
            skipped = 0,
            failed = 0;
        foreach (var recipient in pending)
        {
            var (isSuppressed, reason) = CommunicationSubscriptionPreference.EvaluateSuppression(
                comm.IsBulk,
                comm.TopicKey,
                prefsByDest.GetValueOrDefault(recipient.NormalizedDestination)
            );
            if (isSuppressed)
            {
                recipient.MarkSkippedUnsubscribed(reason ?? "Unsubscribed", nowUtc);
                skipped++;
                continue;
            }

            if (
                await RenderAndSendSingleAsync(
                    comm,
                    recipient,
                    senderPhone,
                    transport,
                    nowUtc,
                    cancellationToken
                )
            )
            {
                delivered++;
            }
            else
            {
                failed++;
            }
        }

        return (delivered, skipped, failed);
    }

    private async Task<bool> RenderAndSendSingleAsync(
        Domain.BulkCommunications.Communication comm,
        CommunicationRecipient recipient,
        string? senderPhone,
        ICommunicationTransport transport,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var outboundResult = await BuildOutboundMessageAsync(
            comm,
            recipient,
            senderPhone,
            cancellationToken
        );
        if (outboundResult.IsFailure)
        {
            recipient.MarkFailed(
                transport.TransportKey,
                outboundResult.PrimaryError.Description,
                nowUtc
            );
            return false;
        }

        var receiptResult = await transport.SendAsync(outboundResult.Value, cancellationToken);
        if (receiptResult.IsFailure)
        {
            recipient.MarkFailed(
                transport.TransportKey,
                receiptResult.PrimaryError.Description,
                nowUtc
            );
            return false;
        }

        recipient.MarkDelivered(
            transport.TransportKey,
            receiptResult.Value.ProviderMessageId,
            nowUtc
        );
        return true;
    }

    private Task<Result<OutboundTransportMessage>> BuildOutboundMessageAsync(
        Domain.BulkCommunications.Communication comm,
        CommunicationRecipient recipient,
        string? senderPhone,
        CancellationToken cancellationToken
    )
    {
        return comm.Channel switch
        {
            CommunicationChannel.Email => BuildEmailOutboundAsync(
                comm,
                recipient,
                cancellationToken
            ),
            CommunicationChannel.Sms => BuildSmsOutboundAsync(
                comm,
                recipient,
                senderPhone,
                cancellationToken
            ),
            CommunicationChannel.Push or _ => BuildPushOutboundAsync(
                comm,
                recipient,
                cancellationToken
            ),
        };
    }

    private async Task<Result<OutboundTransportMessage>> BuildEmailOutboundAsync(
        Domain.BulkCommunications.Communication comm,
        CommunicationRecipient recipient,
        CancellationToken cancellationToken
    )
    {
        var preview = await composerService.RenderEmailAsync(
            comm.Subject ?? comm.Title,
            comm.PreheaderText,
            comm.RazorLayoutKey,
            comm.Sections,
            comm.HtmlBodyOverride,
            comm.IsBulk,
            comm.TopicKey,
            recipient.DisplayName,
            recipient.NormalizedDestination,
            recipient.PersonId,
            recipient.MergeFields,
            cancellationToken
        );
        return preview.IsFailure
            ? Result.Failure<OutboundTransportMessage>(preview.Errors)
            : new OutboundTransportMessage(
                recipient.Id.Value,
                CommunicationChannel.Email,
                recipient.DestinationAddress,
                recipient.DisplayName,
                comm.FromEmail ?? _transportOptions.DefaultFromEmail,
                comm.FromName ?? _transportOptions.DefaultFromName,
                comm.ReplyToEmail,
                preview.Value.RenderedSubject,
                preview.Value.RenderedHtml,
                preview.Value.RenderedPlainText
            );
    }

    private async Task<Result<OutboundTransportMessage>> BuildSmsOutboundAsync(
        Domain.BulkCommunications.Communication comm,
        CommunicationRecipient recipient,
        string? senderPhone,
        CancellationToken cancellationToken
    )
    {
        var sms = await composerService.RenderPlainTemplateAsync(
            comm.SmsBody ?? string.Empty,
            comm.TopicKey,
            recipient.DisplayName,
            recipient.NormalizedDestination,
            recipient.PersonId,
            recipient.MergeFields,
            cancellationToken
        );
        return sms.IsFailure
            ? Result.Failure<OutboundTransportMessage>(sms.Errors)
            : new OutboundTransportMessage(
                recipient.Id.Value,
                CommunicationChannel.Sms,
                recipient.DestinationAddress,
                recipient.DisplayName,
                FromAddress: senderPhone,
                PlainTextBody: sms.Value
            );
    }

    private async Task<Result<OutboundTransportMessage>> BuildPushOutboundAsync(
        Domain.BulkCommunications.Communication comm,
        CommunicationRecipient recipient,
        CancellationToken cancellationToken
    )
    {
        var title = await composerService.RenderPlainTemplateAsync(
            comm.PushTitle ?? comm.Title,
            comm.TopicKey,
            recipient.DisplayName,
            recipient.NormalizedDestination,
            recipient.PersonId,
            recipient.MergeFields,
            cancellationToken
        );
        var body = await composerService.RenderPlainTemplateAsync(
            comm.PushBody ?? string.Empty,
            comm.TopicKey,
            recipient.DisplayName,
            recipient.NormalizedDestination,
            recipient.PersonId,
            recipient.MergeFields,
            cancellationToken
        );
        return title.IsFailure ? Result.Failure<OutboundTransportMessage>(title.Errors)
            : body.IsFailure ? Result.Failure<OutboundTransportMessage>(body.Errors)
            : new OutboundTransportMessage(
                recipient.Id.Value,
                CommunicationChannel.Push,
                recipient.DestinationAddress,
                recipient.DisplayName,
                PlainTextBody: body.Value,
                PushTitle: title.Value,
                PushActionUrl: comm.PushOpenUrl
            );
    }

    private async Task<
        Dictionary<string, CommunicationSubscriptionPreference>
    > LoadPreferencesMapAsync(
        CommunicationChannel channel,
        List<CommunicationRecipient> recipients,
        CancellationToken cancellationToken
    )
    {
        if (recipients.Count == 0)
        {
            return new Dictionary<string, CommunicationSubscriptionPreference>(
                StringComparer.OrdinalIgnoreCase
            );
        }

        var destinations = recipients
            .Select(static r => r.NormalizedDestination)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var prefs = await dbContext
            .CommunicationSubscriptionPreferences.AsNoTracking()
            .Where(p => p.Channel == channel && destinations.Contains(p.NormalizedDestination))
            .ToListAsync(cancellationToken);

        return prefs.ToDictionary(
            static p => p.NormalizedDestination,
            StringComparer.OrdinalIgnoreCase
        );
    }

    private async Task<string?> ResolveSenderPhoneAsync(
        Guid? phoneId,
        CancellationToken cancellationToken
    )
    {
        if (!phoneId.HasValue || phoneId.Value == Guid.Empty)
        {
            return null;
        }

        var id = SystemPhoneNumberId.From(phoneId.Value);
        return await dbContext
            .SystemPhoneNumbers.AsNoTracking()
            .Where(p => p.Id == id && p.IsActive && p.IsSmsEnabled)
            .Select(static p => p.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnqueueCompletionEventAsync(
        Domain.BulkCommunications.Communication comm,
        int batchDelivered,
        int batchSkipped,
        int batchFailed,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var previousStatuses = await dbContext
            .CommunicationRecipients.AsNoTracking()
            .Where(r => r.CommunicationId == comm.Id && r.Status != RecipientDeliveryStatus.Pending)
            .Select(static r => r.Status)
            .ToListAsync(cancellationToken);

        dbContext.EnqueueIntegrationEvent(
            new BulkCommunicationSentIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                comm.Id.Value,
                comm.Title,
                comm.Channel,
                comm.TopicKey,
                batchDelivered
                    + previousStatuses.Count(static s => s == RecipientDeliveryStatus.Delivered),
                batchSkipped
                    + previousStatuses.Count(static s =>
                        s == RecipientDeliveryStatus.SkippedUnsubscribed
                    ),
                batchFailed
                    + previousStatuses.Count(static s => s == RecipientDeliveryStatus.Failed)
            )
        );
    }
}
