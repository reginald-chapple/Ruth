using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class GetEngagementRequestsQueryHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : IQueryHandler<GetEngagementRequestsQuery, IReadOnlyList<EngagementRequestResponse>>
{
    public async Task<Result<IReadOnlyList<EngagementRequestResponse>>> HandleAsync(
        GetEngagementRequestsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext
            .EngagementRequests.AsNoTracking()
            .Include(static r => r.Activities)
            .AsQueryable();

        if (query.PipelineTypeId is { } pid && pid != Guid.Empty)
        {
            q = q.Where(r => r.PipelineTypeId == pid);
        }

        if (query.EngagementOpportunityId is { } oid && oid != Guid.Empty)
        {
            q = q.Where(r => r.EngagementOpportunityId == oid);
        }

        if (query.RequesterPersonId is { } rid && rid != Guid.Empty)
        {
            q = q.Where(r => r.RequesterPersonId == rid);
        }

        if (query.AssignedConnectorPersonId is { } cid && cid != Guid.Empty)
        {
            q = q.Where(r => r.AssignedConnectorPersonId == cid);
        }

        if (query.State.HasValue)
        {
            q = q.Where(r => r.RequestState == query.State.Value);
        }

        var items = await q
            .OrderByDescending(static r => r.LastActivityAtUtc)
            .ToListAsync(cancellationToken);

        var pipelines = await dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .ToDictionaryAsync(static p => p.Id.Value, cancellationToken);
        var oppNames = await dbContext
            .EngagementOpportunities.AsNoTracking()
            .ToDictionaryAsync(static o => o.Id.Value, static o => o.Name, cancellationToken);
        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            items.Select(static r => r.RequesterPersonId),
            cancellationToken
        );

        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        return items
            .Where(r => pipelines.ContainsKey(r.PipelineTypeId))
            .Select(r =>
                PipelineProjectionMapper.ToResponse(
                    r,
                    pipelines[r.PipelineTypeId],
                    oppNames.GetValueOrDefault(r.EngagementOpportunityId, "Opportunity"),
                    people,
                    today,
                    now
                )
            )
            .ToArray();
    }
}
