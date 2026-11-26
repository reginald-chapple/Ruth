using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class GetEngagementOpportunityByIdOrSlugQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetEngagementOpportunityByIdOrSlugQuery, EngagementOpportunityResponse>
{
    public async Task<Result<EngagementOpportunityResponse>> HandleAsync(
        GetEngagementOpportunityByIdOrSlugQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.IdOrSlug))
        {
            return EngagementErrors.OpportunityNotFound;
        }

        var token = query.IdOrSlug.Trim();
        var slug = token.ToLowerInvariant();
        var q = dbContext.EngagementOpportunities.AsNoTracking();
        var entity = Guid.TryParse(token, out var parsedId)
            ? await q.FirstOrDefaultAsync(
                o => o.Id == EngagementOpportunityId.From(parsedId),
                cancellationToken
            )
            : await q.FirstOrDefaultAsync(o => o.Slug == slug, cancellationToken);
        if (entity is null)
        {
            return EngagementErrors.OpportunityNotFound;
        }

        var pipelineName = await dbContext
            .PipelineTypes.AsNoTracking()
            .Where(p => p.Id == PipelineTypeId.From(entity.PipelineTypeId))
            .Select(static p => p.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Pipeline";

        return PipelineProjectionMapper.ToResponse(entity, pipelineName);
    }
}
