using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal static class PipelineRequestResponseLoader
{
    public static async Task<Result<EngagementRequestResponse>> BuildResponseAsync(
        EngagementDbContext dbContext,
        IQueryDispatcher queryDispatcher,
        EngagementRequest request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var pipeline = await dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(request.PipelineTypeId),
                cancellationToken
            );
        if (pipeline is null)
        {
            return EngagementErrors.PipelineTypeNotFound;
        }

        var oppName =
            await dbContext
                .EngagementOpportunities.AsNoTracking()
                .Where(o => o.Id == EngagementOpportunityId.From(request.EngagementOpportunityId))
                .Select(static o => o.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "Opportunity";

        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            [request.RequesterPersonId],
            cancellationToken
        );

        return PipelineProjectionMapper.ToResponse(
            request,
            pipeline,
            oppName,
            people,
            DateOnly.FromDateTime(nowUtc.UtcDateTime),
            nowUtc
        );
    }
}
