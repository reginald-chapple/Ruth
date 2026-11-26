using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class GetContentTopicsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetContentTopicsQuery, IReadOnlyList<ContentTopicResponse>>
{
    public async Task<Result<IReadOnlyList<ContentTopicResponse>>> HandleAsync(
        GetContentTopicsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.ContentTopics.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static t => t.IsActive);
        }

        var list = await q.OrderBy(static t => t.Order)
            .ThenBy(static t => t.Name)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ContentTopicResponse>>([
            .. list.Select(CmsContentProjectionMapper.ToResponse),
        ]);
    }
}
