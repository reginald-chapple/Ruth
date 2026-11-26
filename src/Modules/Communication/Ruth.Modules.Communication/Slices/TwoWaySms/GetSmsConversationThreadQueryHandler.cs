using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class GetSmsConversationThreadQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetSmsConversationThreadQuery, IReadOnlyList<SmsConversationMessageResponse>>
{
    public async Task<Result<IReadOnlyList<SmsConversationMessageResponse>>> HandleAsync(
        GetSmsConversationThreadQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalized = SystemPhoneNumber.NormalizeE164(query.ParticipantPhoneNumber);
        if (normalized.IsFailure)
        {
            return Result.Failure<IReadOnlyList<SmsConversationMessageResponse>>(normalized.Errors);
        }

        var limit = Math.Clamp(query.Limit, 1, 200);
        var messages = await dbContext
            .CommunicationResponses.AsNoTracking()
            .Where(r =>
                r.SystemPhoneNumberId == query.SystemPhoneNumberId
                && r.ParticipantPhoneNumber == normalized.Value
            )
            .OrderByDescending(static r => r.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        messages.Reverse();
        return Result.Success<IReadOnlyList<SmsConversationMessageResponse>>([
            .. messages.Select(TwoWaySmsMapper.ToResponse),
        ]);
    }
}
