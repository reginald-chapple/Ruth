using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class ProcessInboundSmsWebhookCommandHandler(
    CommunicationDbContext dbContext,
    SmsPipelineActionExecutor actionExecutor,
    IClock clock,
    IQueryHandler<SearchPeopleQuery, IReadOnlyList<PersonSummaryResponse>>? searchPeopleHandler =
        null
) : ICommandHandler<ProcessInboundSmsWebhookCommand, InboundSmsProcessResponse>
{
    public async Task<Result<InboundSmsProcessResponse>> HandleAsync(
        ProcessInboundSmsWebhookCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = clock.UtcNow.ToUniversalTime();
        var inboundResult = await BuildInboundResponseAsync(command, nowUtc, cancellationToken);
        if (inboundResult.IsFailure)
        {
            return Result.Failure<InboundSmsProcessResponse>(inboundResult.Errors);
        }

        var inbound = inboundResult.Value;
        _ = dbContext.CommunicationResponses.Add(inbound);

        var activePipelines = await dbContext
            .SmsPipelines.AsNoTracking()
            .Include(static p => p.Actions)
            .Where(static p => p.IsActive)
            .ToListAsync(cancellationToken);

        var (actionCount, replies, optOutChanged) = await actionExecutor.ExecutePipelinesAsync(
            inbound,
            activePipelines,
            nowUtc,
            cancellationToken
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return new InboundSmsProcessResponse(
            inbound.Id.Value,
            inbound.ParticipantPhoneNumber,
            inbound.ParticipantPersonId,
            actionCount,
            replies,
            optOutChanged
        );
    }

    private async Task<Result<CommunicationResponse>> BuildInboundResponseAsync(
        ProcessInboundSmsWebhookCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var fromE164 = SystemPhoneNumber.NormalizeE164(command.FromPhoneNumber);
        if (fromE164.IsFailure)
        {
            return Result.Failure<CommunicationResponse>(fromE164.Errors);
        }

        var toPhoneResult = await ResolveDestinationPhoneAsync(
            command.ToPhoneNumber,
            cancellationToken
        );
        if (toPhoneResult.IsFailure)
        {
            return Result.Failure<CommunicationResponse>(toPhoneResult.Errors);
        }

        var (matchedPersonId, senderName) = await TryMatchCrmSenderAsync(
            fromE164.Value,
            command.SenderDisplayName,
            cancellationToken
        );

        return CommunicationResponse.Create(
            toPhoneResult.Value.Id,
            toPhoneResult.Value.Number,
            fromE164.Value,
            SmsMessageDirection.Inbound,
            command.MessageBody,
            nowUtc,
            matchedPersonId,
            senderName,
            providerMessageId: command.ProviderMessageId
        );
    }

    private async Task<Result<(Guid Id, string Number)>> ResolveDestinationPhoneAsync(
        string? rawToPhone,
        CancellationToken cancellationToken
    )
    {
        var toE164 = SystemPhoneNumber.NormalizeE164(rawToPhone);
        if (toE164.IsFailure)
        {
            return Result.Failure<(Guid, string)>(toE164.Errors);
        }

        var existing = await dbContext
            .SystemPhoneNumbers.AsNoTracking()
            .Where(p => p.Number == toE164.Value)
            .Select(static p => new { p.Id, p.Number })
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return (existing.Id.Value, existing.Number);
        }

        var fallback = await dbContext
            .SystemPhoneNumbers.AsNoTracking()
            .Where(static p => p.IsActive && p.IsSmsEnabled)
            .OrderBy(static p => p.CreatedOnUtc)
            .Select(static p => new { p.Id, p.Number })
            .FirstOrDefaultAsync(cancellationToken);
        return fallback is not null
            ? (fallback.Id.Value, fallback.Number)
            : (Guid.Empty, toE164.Value);
    }

    private async Task<(Guid? PersonId, string DisplayName)> TryMatchCrmSenderAsync(
        string fromE164,
        string? nameHint,
        CancellationToken cancellationToken
    )
    {
        if (searchPeopleHandler is not null)
        {
            var digits = fromE164.TrimStart('+');
            var search = await searchPeopleHandler.HandleAsync(
                new SearchPeopleQuery(Query: digits, MaxResults: 1),
                cancellationToken
            );
            if (search.IsSuccess && search.Value.Count > 0)
            {
                return (search.Value[0].Id, search.Value[0].FullName);
            }
        }

        return (null, string.IsNullOrWhiteSpace(nameHint) ? fromE164 : nameHint.Trim());
    }
}
