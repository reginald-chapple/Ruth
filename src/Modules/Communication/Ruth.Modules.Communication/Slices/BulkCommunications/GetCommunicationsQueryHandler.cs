using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class GetCommunicationsQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetCommunicationsQuery, IReadOnlyList<CommunicationSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<CommunicationSummaryResponse>>> HandleAsync(
        GetCommunicationsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var limit = Math.Clamp(query.Limit, 1, 200);
        var q = dbContext.Communications.AsNoTracking();
        if (query.Status.HasValue)
        {
            q = q.Where(c => c.Status == query.Status.Value);
        }

        if (query.Channel.HasValue)
        {
            q = q.Where(c => c.Channel == query.Channel.Value);
        }

        var comms = await q.OrderByDescending(static c => c.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
        if (comms.Count == 0)
        {
            return Result.Success<IReadOnlyList<CommunicationSummaryResponse>>([]);
        }

        var ids = comms.Select(static c => c.Id).ToArray();
        var recipients = await dbContext
            .CommunicationRecipients.AsNoTracking()
            .Where(r => ids.Contains(r.CommunicationId))
            .ToListAsync(cancellationToken);
        var byComm = recipients
            .GroupBy(static r => r.CommunicationId)
            .ToDictionary(
                static g => g.Key,
                static g => (IReadOnlyList<Domain.BulkCommunications.CommunicationRecipient>)[.. g]
            );

        var list = comms
            .Select(c =>
                BulkCommunicationMapper.ToSummaryResponse(c, byComm.GetValueOrDefault(c.Id) ?? [])
            )
            .ToList();
        return Result.Success<IReadOnlyList<CommunicationSummaryResponse>>(list);
    }
}
