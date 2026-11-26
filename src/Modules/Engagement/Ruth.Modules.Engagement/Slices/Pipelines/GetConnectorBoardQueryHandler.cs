using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class GetConnectorBoardQueryHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : IQueryHandler<GetConnectorBoardQuery, ConnectorBoardResponse>
{
    public async Task<Result<ConnectorBoardResponse>> HandleAsync(
        GetConnectorBoardQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var pipeline = await dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(query.PipelineTypeId),
                cancellationToken
            );
        if (pipeline is null)
        {
            return EngagementErrors.PipelineTypeNotFound;
        }

        var requests = await FilterBoardRequests(query)
            .OrderByDescending(static r => r.LastActivityAtUtc)
            .ToListAsync(cancellationToken);
        var oppNames = await dbContext
            .EngagementOpportunities.AsNoTracking()
            .Where(o => o.PipelineTypeId == query.PipelineTypeId)
            .ToDictionaryAsync(static o => o.Id.Value, static o => o.Name, cancellationToken);
        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            requests.Select(static r => r.RequesterPersonId),
            cancellationToken
        );

        return BuildBoardResponse(pipeline, requests, oppNames, people);
    }

    private IQueryable<EngagementRequest> FilterBoardRequests(GetConnectorBoardQuery query)
    {
        var reqQuery = dbContext
            .EngagementRequests.AsNoTracking()
            .Include(static r => r.Activities)
            .Where(r => r.PipelineTypeId == query.PipelineTypeId);
        if (query.EngagementOpportunityId is { } oppId && oppId != Guid.Empty)
        {
            reqQuery = reqQuery.Where(r => r.EngagementOpportunityId == oppId);
        }

        if (query.BranchId is { } bid && bid != Guid.Empty)
        {
            reqQuery = reqQuery.Where(r => r.BranchId == bid);
        }

        if (query.AssignedConnectorPersonId is { } cid && cid != Guid.Empty)
        {
            reqQuery = reqQuery.Where(r => r.AssignedConnectorPersonId == cid);
        }

        return query.IncludeClosed
            ? reqQuery
            : reqQuery.Where(static r =>
                r.RequestState == EngagementRequestState.Active
                || r.RequestState == EngagementRequestState.FutureFollowUp
            );
    }

    private ConnectorBoardResponse BuildBoardResponse(
        PipelineType pipeline,
        List<EngagementRequest> requests,
        Dictionary<Guid, string> oppNames,
        IReadOnlyDictionary<Guid, (string Name, string? Email)> people
    )
    {
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var mapped = requests
            .Select(r =>
                PipelineProjectionMapper.ToResponse(
                    r,
                    pipeline,
                    oppNames.GetValueOrDefault(r.EngagementOpportunityId, "Opportunity"),
                    people,
                    today,
                    now
                )
            )
            .ToArray();

        var columns = pipeline
            .Stages.OrderBy(static s => s.Order)
            .Select(s => new ConnectorBoardColumnDto(
                s.Id.Value,
                s.Name,
                s.Order,
                s.IsTerminalCompleted,
                s.IsTerminalInactive,
                [.. mapped.Where(m => m.CurrentStageId == s.Id.Value)]
            ))
            .ToArray();

        return new ConnectorBoardResponse(
            pipeline.Id.Value,
            pipeline.Name,
            mapped.Count(static m => m.RequestState == EngagementRequestState.Active),
            mapped.Count(static m =>
                m.Health is EngagementRequestHealth.Idle or EngagementRequestHealth.OverdueFollowUp
            ),
            mapped.Count(static m => m.RequestState == EngagementRequestState.FutureFollowUp),
            mapped.Count(static m => m.RequestState == EngagementRequestState.Connected),
            columns,
            mapped
        );
    }
}
