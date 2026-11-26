using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class SmsPipelineActionExecutor(
    CommunicationDbContext dbContext,
    IMergeTemplateEngine mergeEngine,
    CommunicationTransportResolver transportResolver,
    ICommandDispatcher commandDispatcher,
    IOptions<CommunicationTransportOptions> transportOptions
)
{
    private readonly CommunicationTransportOptions _transportOptions = transportOptions.Value;

    public async Task<(
        int ActionsRun,
        List<string> Replies,
        bool OptOutChanged
    )> ExecutePipelinesAsync(
        CommunicationResponse inbound,
        IReadOnlyList<SmsPipeline> activePipelines,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var replies = new List<string>();
        if (SmsKeywordMatcher.IsMandatoryOptOutKeyword(inbound.MessageBody))
        {
            inbound.SetMatchedKeyword(inbound.MessageBody.Trim().ToUpperInvariant());
            await SetSmsOptOutAsync(inbound, optOut: true, "SmsStopKeyword", cancellationToken);
            var msg =
                $"You have been unsubscribed from {_transportOptions.DefaultOrganizationName} SMS messages. Reply START to re-subscribe.";
            await DispatchAutoReplyAsync(inbound, msg, nowUtc, cancellationToken);
            replies.Add(msg);
            return (1, replies, true);
        }

        if (SmsKeywordMatcher.IsMandatoryOptInKeyword(inbound.MessageBody))
        {
            inbound.SetMatchedKeyword(inbound.MessageBody.Trim().ToUpperInvariant());
            await SetSmsOptOutAsync(inbound, optOut: false, "SmsStartKeyword", cancellationToken);
            var msg =
                $"You are now subscribed to {_transportOptions.DefaultOrganizationName} SMS updates. Reply STOP to opt out.";
            await DispatchAutoReplyAsync(inbound, msg, nowUtc, cancellationToken);
            replies.Add(msg);
            return (1, replies, true);
        }

        var matched = SmsKeywordMatcher.MatchActions(
            inbound.SystemPhoneNumberId,
            inbound.MessageBody,
            activePipelines
        );
        var optOutChanged = false;
        foreach (var action in matched)
        {
            inbound.SetMatchedKeyword(action.Keyword ?? "ANY");
            optOutChanged |= await ExecuteSingleSmsActionAsync(
                inbound,
                action,
                replies,
                nowUtc,
                cancellationToken
            );
        }

        return (matched.Count, replies, optOutChanged);
    }

    private async Task<bool> ExecuteSingleSmsActionAsync(
        CommunicationResponse inbound,
        SmsAction action,
        List<string> replies,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var optOutChanged = false;
        if (action.ActionKind == SmsActionKind.OptOutSms)
        {
            await SetSmsOptOutAsync(
                inbound,
                optOut: true,
                $"Pipeline:{action.Name}",
                cancellationToken
            );
            optOutChanged = true;
        }
        else if (action.ActionKind == SmsActionKind.OptInSms)
        {
            await SetSmsOptOutAsync(
                inbound,
                optOut: false,
                $"Pipeline:{action.Name}",
                cancellationToken
            );
            optOutChanged = true;
        }
        else if (
            action.ActionKind == SmsActionKind.PublishSystemNotification
            && !string.IsNullOrWhiteSpace(action.TargetSystemCommunicationKey)
        )
        {
            _ = await commandDispatcher.SendAsync<
                SendSystemCommunicationCommand,
                SystemCommunicationSendResultResponse
            >(
                new SendSystemCommunicationCommand(
                    action.TargetSystemCommunicationKey,
                    RecipientName: inbound.ParticipantDisplayName,
                    RecipientPhoneNumber: inbound.ParticipantPhoneNumber,
                    RecipientPersonId: inbound.ParticipantPersonId,
                    TriggerSource: $"SmsPipeline:{action.Name}"
                ),
                cancellationToken
            );
        }

        if (!string.IsNullOrWhiteSpace(action.ReplyTemplate))
        {
            var replyText = await RenderReplyAsync(
                inbound,
                action.ReplyTemplate,
                cancellationToken
            );
            if (!string.IsNullOrWhiteSpace(replyText))
            {
                await DispatchAutoReplyAsync(inbound, replyText, nowUtc, cancellationToken);
                replies.Add(replyText);
            }
        }

        return optOutChanged;
    }

    private async Task<string?> RenderReplyAsync(
        CommunicationResponse inbound,
        string template,
        CancellationToken cancellationToken
    )
    {
        var context = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrganizationName"] = _transportOptions.DefaultOrganizationName,
            ["RecipientName"] = inbound.ParticipantDisplayName,
            ["FromPhone"] = inbound.ParticipantPhoneNumber,
            ["InboundBody"] = inbound.MessageBody,
        };
        var res = await mergeEngine.RenderAsync(
            template,
            context,
            MergeRenderOptions.DefaultPlainText,
            cancellationToken
        );
        return res.IsSuccess ? res.Value.RenderedContent.Trim() : null;
    }

    private async Task DispatchAutoReplyAsync(
        CommunicationResponse inbound,
        string replyBody,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var transport = transportResolver.ResolveForChannel(CommunicationChannel.Sms);
        var autoResponse = CommunicationResponse.Create(
            inbound.SystemPhoneNumberId,
            inbound.SystemPhoneNumber,
            inbound.ParticipantPhoneNumber,
            SmsMessageDirection.AutomatedReply,
            replyBody,
            nowUtc,
            inbound.ParticipantPersonId,
            inbound.ParticipantDisplayName,
            inbound.MatchedKeyword
        );
        if (autoResponse.IsFailure)
        {
            return;
        }

        _ = await transport.SendAsync(
            new OutboundTransportMessage(
                autoResponse.Value.Id.Value,
                CommunicationChannel.Sms,
                inbound.ParticipantPhoneNumber,
                inbound.ParticipantDisplayName,
                FromAddress: inbound.SystemPhoneNumber,
                PlainTextBody: replyBody
            ),
            cancellationToken
        );
        _ = dbContext.CommunicationResponses.Add(autoResponse.Value);
    }

    private async Task SetSmsOptOutAsync(
        CommunicationResponse inbound,
        bool optOut,
        string reason,
        CancellationToken cancellationToken
    )
    {
        var pref = await dbContext.CommunicationSubscriptionPreferences.FirstOrDefaultAsync(
            p =>
                p.Channel == CommunicationChannel.Sms
                && p.NormalizedDestination == inbound.ParticipantPhoneNumber,
            cancellationToken
        );
        if (pref is null)
        {
            var created = CommunicationSubscriptionPreference.Create(
                CommunicationChannel.Sms,
                inbound.ParticipantPhoneNumber,
                inbound.ParticipantPersonId
            );
            if (created.IsFailure)
            {
                return;
            }

            pref = created.Value;
            _ = dbContext.CommunicationSubscriptionPreferences.Add(pref);
        }

        pref.ApplyPreferences(
            globalOptOut: optOut,
            optedOutTopics: pref.OptedOutTopics,
            reason: reason,
            personId: inbound.ParticipantPersonId
        );
    }
}
