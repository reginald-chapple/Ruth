using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class GetPipelineTypesQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetPipelineTypesQuery, IReadOnlyList<PipelineTypeResponse>>
{
    public async Task<Result<IReadOnlyList<PipelineTypeResponse>>> HandleAsync(
        GetPipelineTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext
            .PipelineTypes.AsNoTracking()
            .Include(static p => p.Stages)
            .Include(static p => p.ActivityTypes)
            .AsQueryable();
        if (query.ActiveOnly)
        {
            q = q.Where(static p => p.IsActive);
        }

        var items = await q.OrderBy(static p => p.Name).ToListAsync(cancellationToken);
        return items.Select(PipelineProjectionMapper.ToResponse).ToArray();
    }
}
