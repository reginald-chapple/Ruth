using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class SendSystemCommunicationCommandHandler(
    CommunicationDbContext dbContext,
    SystemCommunicationRecipientResolver recipientResolver,
    SystemCommunicationRenderService renderService,
    CommunicationTransportResolver transportResolver,
    IOptions<CommunicationTransportOptions> transportOptions,
    IClock clock
) : ICommandHandler<SendSystemCommunicationCommand, SystemCommunicationSendResultResponse>
{
    private readonly CommunicationTransportOptions _options = transportOptions.Value;

    public async Task<Result<SystemCommunicationSendResultResponse>> HandleAsync(
        SendSystemCommunicationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (
            !string.IsNullOrWhiteSpace(command.RecipientEmail)
            && !EmailHeaderSanitizer.TrySanitizeEmailAddress(command.RecipientEmail, out _)
        )
        {
            return DomainError.Validation(
                "Communication.EmailHeaderInjection",
                "Recipient email address contains prohibited CR/LF or header-injection characters."
            );
        }

        var templateResult = await LoadActiveTemplateAsync(
            command.SystemCommunicationKey,
            cancellationToken
        );
        if (templateResult.IsFailure)
        {
            return Result.Failure<SystemCommunicationSendResultResponse>(templateResult.Errors);
        }

        var template = templateResult.Value;
        var (email, name, phone) = await recipientResolver.ResolveRecipientAsync(
            command,
            cancellationToken
        );
        var rendered = await renderService.RenderAsync(
            template,
            name,
            command.MergeFields,
            cancellationToken
        );
        return rendered.IsFailure
            ? Result.Failure<SystemCommunicationSendResultResponse>(rendered.Errors)
            : await DispatchAllChannelsAsync(
                template,
                rendered.Value,
                email,
                name,
                phone,
                command,
                cancellationToken
            );
    }

    private async Task<Result<SystemCommunication>> LoadActiveTemplateAsync(
        string rawKey,
        CancellationToken cancellationToken
    )
    {
        var keyResult = SystemCommunication.NormalizeKey(rawKey);
        if (keyResult.IsFailure)
        {
            return Result.Failure<SystemCommunication>(keyResult.Errors);
        }

        var template = await dbContext
            .SystemCommunications.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == keyResult.Value, cancellationToken);
        return template is null ? CommunicationDomainErrors.SystemCommunicationNotFound
            : !template.IsActive ? CommunicationDomainErrors.SystemCommunicationInactive
            : template;
    }

    private async Task<SystemCommunicationSendResultResponse> DispatchAllChannelsAsync(
        SystemCommunication template,
        RenderedNotificationBundle bundle,
        string? email,
        string? name,
        string? phone,
        SendSystemCommunicationCommand command,
        CancellationToken cancellationToken
    )
    {
        var receipts = new List<TransportDeliveryReceipt>(4);
        await DispatchEmailIfEligibleAsync(
            template,
            bundle,
            email,
            name,
            command,
            receipts,
            cancellationToken
        );
        await DispatchSmsIfEligibleAsync(
            template,
            bundle,
            phone,
            command,
            receipts,
            cancellationToken
        );
        await DispatchPushIfEligibleAsync(template, bundle, command, receipts, cancellationToken);

        if (receipts.Count > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SystemCommunicationSendResultResponse(
            template.Id.Value,
            template.Key,
            receipts.Count,
            receipts,
            email is not null ? bundle.HtmlBody : null,
            phone is not null ? bundle.SmsBody : null,
            bundle.PushBody
        );
    }

    private async Task DispatchEmailIfEligibleAsync(
        SystemCommunication template,
        RenderedNotificationBundle bundle,
        string? recipientEmail,
        string? recipientName,
        SendSystemCommunicationCommand command,
        List<TransportDeliveryReceipt> receipts,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        var transport = transportResolver.ResolveForChannel(CommunicationChannel.Email);
        var msg = new OutboundTransportMessage(
            Guid.CreateVersion7(),
            CommunicationChannel.Email,
            recipientEmail,
            recipientName,
            template.FromEmail ?? _options.DefaultFromEmail,
            template.FromName ?? _options.DefaultFromName,
            Subject: bundle.Subject,
            HtmlBody: bundle.HtmlBody,
            PlainTextBody: bundle.PlainTextBody,
            SystemCommunicationKey: template.Key
        );

        var sent = await transport.SendAsync(msg, cancellationToken);
        if (sent.IsSuccess)
        {
            RecordReceiptAndAudit(
                template,
                command,
                recipientEmail,
                bundle.Subject,
                sent.Value,
                receipts
            );
        }
    }

    private async Task DispatchSmsIfEligibleAsync(
        SystemCommunication template,
        RenderedNotificationBundle bundle,
        string? recipientPhone,
        SendSystemCommunicationCommand command,
        List<TransportDeliveryReceipt> receipts,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(recipientPhone) || string.IsNullOrWhiteSpace(bundle.SmsBody))
        {
            return;
        }

        var fromNumber = await ResolveSmsSenderNumberAsync(template, cancellationToken);
        var transport = transportResolver.ResolveForChannel(CommunicationChannel.Sms);
        var msg = new OutboundTransportMessage(
            Guid.CreateVersion7(),
            CommunicationChannel.Sms,
            recipientPhone,
            FromAddress: fromNumber,
            PlainTextBody: bundle.SmsBody,
            SystemCommunicationKey: template.Key
        );

        var sent = await transport.SendAsync(msg, cancellationToken);
        if (sent.IsSuccess)
        {
            RecordReceiptAndAudit(
                template,
                command,
                recipientPhone,
                bundle.SmsBody,
                sent.Value,
                receipts
            );
        }
    }

    private async Task DispatchPushIfEligibleAsync(
        SystemCommunication template,
        RenderedNotificationBundle bundle,
        SendSystemCommunicationCommand command,
        List<TransportDeliveryReceipt> receipts,
        CancellationToken cancellationToken
    )
    {
        if (
            !command.RecipientPersonId.HasValue
            || command.RecipientPersonId.Value == Guid.Empty
            || string.IsNullOrWhiteSpace(bundle.PushBody)
        )
        {
            return;
        }

        var tokens = await dbContext
            .PersonalDevices.AsNoTracking()
            .Where(d =>
                d.PersonId == command.RecipientPersonId.Value
                && d.IsActive
                && d.NotificationsEnabled
            )
            .Select(static d => d.PushRegistrationToken)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

        var transport = transportResolver.ResolveForChannel(CommunicationChannel.Push);
        foreach (var token in tokens)
        {
            var msg = new OutboundTransportMessage(
                Guid.CreateVersion7(),
                CommunicationChannel.Push,
                token,
                PlainTextBody: bundle.PushBody,
                PushTitle: bundle.PushTitle,
                PushActionUrl: bundle.PushActionUrl,
                SystemCommunicationKey: template.Key
            );

            var sent = await transport.SendAsync(msg, cancellationToken);
            if (sent.IsSuccess)
            {
                RecordReceiptAndAudit(
                    template,
                    command,
                    token,
                    bundle.PushTitle,
                    sent.Value,
                    receipts
                );
            }
        }
    }

    private async Task<string?> ResolveSmsSenderNumberAsync(
        SystemCommunication template,
        CancellationToken cancellationToken
    )
    {
        if (template.SmsFromSystemPhoneNumberId.HasValue)
        {
            var id = SystemPhoneNumberId.From(template.SmsFromSystemPhoneNumberId.Value);
            var number = await dbContext
                .SystemPhoneNumbers.AsNoTracking()
                .Where(p => p.Id == id && p.IsActive && p.IsSmsEnabled)
                .Select(static p => p.Number)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(number))
            {
                return number;
            }
        }

        return await dbContext
            .SystemPhoneNumbers.AsNoTracking()
            .Where(static p => p.IsActive && p.IsSmsEnabled)
            .OrderBy(static p => p.Name)
            .Select(static p => p.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private void RecordReceiptAndAudit(
        SystemCommunication template,
        SendSystemCommunicationCommand command,
        string recipientAddress,
        string? subjectOrTitle,
        TransportDeliveryReceipt receipt,
        List<TransportDeliveryReceipt> receipts
    )
    {
        receipts.Add(receipt);
        _ = dbContext.SystemNotificationLogs.Add(
            SystemNotificationLog.Create(
                template.Id.Value,
                template.Key,
                receipt.Channel,
                receipt.TransportKey,
                recipientAddress,
                command.RecipientPersonId,
                subjectOrTitle,
                receipt.ProviderMessageId,
                command.TriggerSource,
                clock.UtcNow
            )
        );
    }
}
