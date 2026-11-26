using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class GetSmsConversationsQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetSmsConversationsQuery, IReadOnlyList<SmsConversationSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<SmsConversationSummaryResponse>>> HandleAsync(
        GetSmsConversationsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = Math.Clamp(query.Limit, 1, 100);
        var q = dbContext.CommunicationResponses.AsNoTracking();
        if (query.SystemPhoneNumberId.HasValue && query.SystemPhoneNumberId.Value != Guid.Empty)
        {
            q = q.Where(r => r.SystemPhoneNumberId == query.SystemPhoneNumberId.Value);
        }

        var recentMessages = await q.OrderByDescending(static r => r.OccurredAtUtc)
            .Take(limit * 20)
            .ToListAsync(cancellationToken);

        var summaries = recentMessages
            .GroupBy(static r => (r.SystemPhoneNumberId, r.ParticipantPhoneNumber))
            .Select(static g =>
            {
                var latest = g.First();
                var unread = g.Count(static m =>
                    m.Direction == SmsMessageDirection.Inbound && !m.IsRead
                );
                var preview =
                    latest.MessageBody.Length <= 80
                        ? latest.MessageBody
                        : $"{latest.MessageBody[..80]}…";
                return new SmsConversationSummaryResponse(
                    latest.SystemPhoneNumberId,
                    latest.SystemPhoneNumber,
                    latest.ParticipantPhoneNumber,
                    latest.ParticipantPersonId,
                    latest.ParticipantDisplayName,
                    preview,
                    latest.OccurredAtUtc,
                    unread,
                    g.Count()
                );
            })
            .Where(s => !query.UnreadOnly || s.UnreadInboundCount > 0)
            .OrderByDescending(static s => s.LastMessageAtUtc)
            .Take(limit)
            .ToList();

        return Result.Success<IReadOnlyList<SmsConversationSummaryResponse>>(summaries);
    }
}
