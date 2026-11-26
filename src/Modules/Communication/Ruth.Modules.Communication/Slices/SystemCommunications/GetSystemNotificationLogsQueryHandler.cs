using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class GetSystemNotificationLogsQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetSystemNotificationLogsQuery, IReadOnlyList<SystemNotificationLogResponse>>
{
    public async Task<Result<IReadOnlyList<SystemNotificationLogResponse>>> HandleAsync(
        GetSystemNotificationLogsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var boundedLimit = Math.Clamp(query.Limit, 1, 200);
        var dbQuery = dbContext.SystemNotificationLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.SystemCommunicationKey))
        {
            var key = query.SystemCommunicationKey.Trim();
            dbQuery = dbQuery.Where(l => l.SystemCommunicationKey == key);
        }

        if (query.Channel.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.Channel == query.Channel.Value);
        }

        var logs = await dbQuery
            .OrderByDescending(static l => l.SentOnUtc)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);

        IReadOnlyList<SystemNotificationLogResponse> items =
        [
            .. logs.Select(SystemCommunicationMapper.ToLogResponse),
        ];
        return Result.Success(items);
    }
}
