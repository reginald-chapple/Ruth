using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Templating;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.Modules.Communication.Slices.BulkCommunications;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class FlowStepSenderCollaborator(
    CommunicationDbContext dbContext,
    BulkEmailComposerService composerService,
    CommunicationTransportResolver transportResolver,
    IOptions<CommunicationTransportOptions> transportOptions
)
{
    private readonly CommunicationTransportOptions _transportOptions = transportOptions.Value;

    public async Task<bool> IsRecipientSuppressedAsync(
        CommunicationFlow flow,
        CommunicationFlowStep step,
        CommunicationFlowInstanceRecipient recipient,
        CancellationToken cancellationToken
    )
    {
        var dest =
            step.Channel == CommunicationChannel.Email
                ? recipient.NormalizedEmail
                : recipient.PhoneNumber;
        if (string.IsNullOrWhiteSpace(dest))
        {
            return false;
        }

        var pref = await dbContext
            .CommunicationSubscriptionPreferences.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Channel == step.Channel && p.NormalizedDestination == dest,
                cancellationToken
            );
        return CommunicationSubscriptionPreference
            .EvaluateSuppression(isBulk: true, flow.TopicKey, pref)
            .IsSuppressed;
    }

    public async Task<Result> SendStepAsync(
        CommunicationFlow flow,
        CommunicationFlowStep step,
        CommunicationFlowInstanceRecipient recipient,
        CancellationToken cancellationToken
    )
    {
        var transport = transportResolver.ResolveForChannel(step.Channel);
        var outboundResult = await BuildStepMessageAsync(flow, step, recipient, cancellationToken);
        if (outboundResult.IsFailure)
        {
            return Result.Failure(outboundResult.Errors);
        }

        var receipt = await transport.SendAsync(outboundResult.Value, cancellationToken);
        return receipt.IsFailure ? Result.Failure(receipt.Errors) : Result.Success();
    }

    private Task<Result<OutboundTransportMessage>> BuildStepMessageAsync(
        CommunicationFlow flow,
        CommunicationFlowStep step,
        CommunicationFlowInstanceRecipient recipient,
        CancellationToken cancellationToken
    )
    {
        return step.Channel == CommunicationChannel.Email
            ? BuildEmailStepAsync(flow, step, recipient, cancellationToken)
            : BuildSmsOrPushStepAsync(flow, step, recipient, cancellationToken);
    }

    private async Task<Result<OutboundTransportMessage>> BuildEmailStepAsync(
        CommunicationFlow flow,
        CommunicationFlowStep step,
        CommunicationFlowInstanceRecipient recipient,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(recipient.EmailAddress))
        {
            return Result.Failure<OutboundTransportMessage>(
                BulkCommunicationErrors.RecipientDestinationInvalid
            );
        }

        var (subject, sections, htmlOverride, layoutKey) = await ResolveEmailContentAsync(
            flow,
            step,
            cancellationToken
        );
        var preview = await composerService.RenderEmailAsync(
            subject,
            preheaderText: null,
            layoutKey,
            sections,
            htmlOverride,
            includeUnsubscribeFooter: true,
            flow.TopicKey,
            recipient.DisplayName,
            recipient.NormalizedEmail,
            recipient.PersonId,
            recipient.MergeFields,
            cancellationToken
        );
        return preview.IsFailure
            ? Result.Failure<OutboundTransportMessage>(preview.Errors)
            : new OutboundTransportMessage(
                Guid.CreateVersion7(),
                CommunicationChannel.Email,
                recipient.EmailAddress,
                recipient.DisplayName,
                _transportOptions.DefaultFromEmail,
                _transportOptions.DefaultFromName,
                Subject: preview.Value.RenderedSubject,
                HtmlBody: preview.Value.RenderedHtml,
                PlainTextBody: preview.Value.RenderedPlainText
            );
    }

    private async Task<(
        string Subject,
        IReadOnlyList<EmailSection> Sections,
        string? HtmlOverride,
        string? LayoutKey
    )> ResolveEmailContentAsync(
        CommunicationFlow flow,
        CommunicationFlowStep step,
        CancellationToken cancellationToken
    )
    {
        if (step.CommunicationTemplateId.HasValue)
        {
            var tid = CommunicationTemplateId.From(step.CommunicationTemplateId.Value);
            var tpl = await dbContext
                .CommunicationTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tid, cancellationToken);
            if (tpl is not null)
            {
                return (
                    step.SubjectTemplate ?? tpl.SubjectTemplate ?? step.Name,
                    tpl.Sections,
                    step.BodyTemplate,
                    tpl.RazorLayoutKey
                );
            }
        }

        return (
            step.SubjectTemplate ?? $"{flow.Name} — {step.Name}",
            [],
            step.BodyTemplate,
            BuiltInRazorTemplatesCatalog.StandardEmailKey
        );
    }

    private async Task<Result<OutboundTransportMessage>> BuildSmsOrPushStepAsync(
        CommunicationFlow flow,
        CommunicationFlowStep step,
        CommunicationFlowInstanceRecipient recipient,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(recipient.PhoneNumber))
        {
            return Result.Failure<OutboundTransportMessage>(
                BulkCommunicationErrors.RecipientDestinationInvalid
            );
        }

        var body = await composerService.RenderPlainTemplateAsync(
            step.BodyTemplate ?? step.Name,
            flow.TopicKey,
            recipient.DisplayName,
            recipient.PhoneNumber,
            recipient.PersonId,
            recipient.MergeFields,
            cancellationToken
        );
        return body.IsFailure
            ? Result.Failure<OutboundTransportMessage>(body.Errors)
            : new OutboundTransportMessage(
                Guid.CreateVersion7(),
                step.Channel,
                recipient.PhoneNumber,
                recipient.DisplayName,
                PlainTextBody: body.Value,
                PushTitle: step.SubjectTemplate
            );
    }
}
