using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class SendSmsConversationReplyCommandHandler(
    CommunicationDbContext dbContext,
    CommunicationTransportResolver transportResolver,
    IClock clock
) : ICommandHandler<SendSmsConversationReplyCommand, SmsConversationMessageResponse>
{
    public async Task<Result<SmsConversationMessageResponse>> HandleAsync(
        SendSmsConversationReplyCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var validated = await ValidateSenderAndOptOutAsync(command, cancellationToken);
        return validated.IsFailure
            ? Result.Failure<SmsConversationMessageResponse>(validated.Errors)
            : await DispatchAndPersistReplyAsync(
                command,
                validated.Value.SystemPhone,
                validated.Value.NormalizedTo,
                cancellationToken
            );
    }

    private async Task<
        Result<(SystemPhoneNumber SystemPhone, string NormalizedTo)>
    > ValidateSenderAndOptOutAsync(
        SendSmsConversationReplyCommand command,
        CancellationToken cancellationToken
    )
    {
        var phoneId = SystemPhoneNumberId.From(command.SystemPhoneNumberId);
        var systemPhone = await dbContext
            .SystemPhoneNumbers.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == phoneId && p.IsActive && p.IsSmsEnabled,
                cancellationToken
            );
        if (systemPhone is null)
        {
            return Result.Failure<(SystemPhoneNumber, string)>(
                CommunicationDomainErrors.SystemPhoneNumberNotFound
            );
        }

        var normalizedTo = SystemPhoneNumber.NormalizeE164(command.ParticipantPhoneNumber);
        if (normalizedTo.IsFailure)
        {
            return Result.Failure<(SystemPhoneNumber, string)>(normalizedTo.Errors);
        }

        var optedOut = await dbContext.CommunicationSubscriptionPreferences.AnyAsync(
            p =>
                p.Channel == CommunicationChannel.Sms
                && p.NormalizedDestination == normalizedTo.Value
                && p.GlobalOptOut,
            cancellationToken
        );
        return optedOut
            ? Result.Failure<(SystemPhoneNumber, string)>(
                BulkCommunicationErrors.SmsRecipientOptedOut
            )
            : (systemPhone, normalizedTo.Value);
    }

    private async Task<Result<SmsConversationMessageResponse>> DispatchAndPersistReplyAsync(
        SendSmsConversationReplyCommand command,
        SystemPhoneNumber systemPhone,
        string normalizedTo,
        CancellationToken cancellationToken
    )
    {
        var prior = await dbContext
            .CommunicationResponses.Where(r =>
                r.SystemPhoneNumberId == command.SystemPhoneNumberId
                && r.ParticipantPhoneNumber == normalizedTo
            )
            .OrderByDescending(static r => r.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var unread in prior.Where(static r => !r.IsRead))
        {
            unread.MarkRead();
        }

        var replyResult = CommunicationResponse.Create(
            systemPhone.Id.Value,
            systemPhone.Number,
            normalizedTo,
            SmsMessageDirection.OutboundReply,
            command.MessageBody,
            clock.UtcNow.ToUniversalTime(),
            prior.FirstOrDefault()?.ParticipantPersonId,
            prior.FirstOrDefault()?.ParticipantDisplayName,
            repliedByPersonId: command.RepliedByPersonId
        );
        if (replyResult.IsFailure)
        {
            return Result.Failure<SmsConversationMessageResponse>(replyResult.Errors);
        }

        var transport = transportResolver.ResolveForChannel(CommunicationChannel.Sms);
        var sendResult = await transport.SendAsync(
            new OutboundTransportMessage(
                replyResult.Value.Id.Value,
                CommunicationChannel.Sms,
                normalizedTo,
                replyResult.Value.ParticipantDisplayName,
                FromAddress: systemPhone.Number,
                PlainTextBody: replyResult.Value.MessageBody
            ),
            cancellationToken
        );
        if (sendResult.IsFailure)
        {
            return Result.Failure<SmsConversationMessageResponse>(sendResult.Errors);
        }

        _ = dbContext.CommunicationResponses.Add(replyResult.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return TwoWaySmsMapper.ToResponse(replyResult.Value);
    }
}
