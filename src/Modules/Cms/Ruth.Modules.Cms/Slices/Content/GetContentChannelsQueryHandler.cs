using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class GetContentChannelsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetContentChannelsQuery, IReadOnlyList<ContentChannelResponse>>
{
    public async Task<Result<IReadOnlyList<ContentChannelResponse>>> HandleAsync(
        GetContentChannelsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.ContentChannels.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static c => c.IsActive);
        }

        var list = await q.OrderBy(static c => c.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ContentChannelResponse>>([
            .. list.Select(CmsContentProjectionMapper.ToResponse),
        ]);
    }
}
