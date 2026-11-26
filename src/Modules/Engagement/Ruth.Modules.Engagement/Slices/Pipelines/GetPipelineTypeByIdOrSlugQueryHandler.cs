using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class GetPipelineTypeByIdOrSlugQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetPipelineTypeByIdOrSlugQuery, PipelineTypeResponse>
{
    public async Task<Result<PipelineTypeResponse>> HandleAsync(
        GetPipelineTypeByIdOrSlugQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.IdOrSlug))
        {
            return EngagementErrors.PipelineTypeNotFound;
        }

        var token = query.IdOrSlug.Trim();
        var q = dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .Include(static p => p.ActivityTypes);

        var entity = Guid.TryParse(token, out var parsedId)
            ? await q.FirstOrDefaultAsync(
                p => p.Id == PipelineTypeId.From(parsedId),
                cancellationToken
            )
            : await q.FirstOrDefaultAsync(
                p => p.Slug == token.ToLowerInvariant(),
                cancellationToken
            );

        return entity is null
            ? EngagementErrors.PipelineTypeNotFound
            : PipelineProjectionMapper.ToResponse(entity);
    }
}
