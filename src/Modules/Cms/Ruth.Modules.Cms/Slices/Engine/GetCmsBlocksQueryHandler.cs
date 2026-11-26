using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetCmsBlocksQueryHandler(CmsDbContext db)
    : IQueryHandler<GetCmsBlocksQuery, IReadOnlyList<CmsBlockResponse>>
{
    public async Task<Result<IReadOnlyList<CmsBlockResponse>>> HandleAsync(
        GetCmsBlocksQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.Blocks.AsNoTracking();
        if (query.PageId is { } pid)
        {
            var pageId = CmsPageId.From(pid);
            q = q.Where(b => b.PageId == pageId);
        }

        if (query.LayoutId is { } lid)
        {
            var layoutId = CmsLayoutId.From(lid);
            q = q.Where(b => b.LayoutId == layoutId);
        }

        if (query.SiteId is { } sid)
        {
            var siteId = CmsSiteId.From(sid);
            q = q.Where(b => b.SiteId == siteId);
        }

        if (!query.IncludeInactive)
        {
            q = q.Where(static b => b.IsActive);
        }

        var blocks = await q.OrderBy(static b => b.Zone)
            .ThenBy(static b => b.Order)
            .ToListAsync(cancellationToken);
        var blockTypes = await db
            .BlockTypes.AsNoTracking()
            .ToDictionaryAsync(static bt => bt.Id, cancellationToken);

        List<CmsBlockResponse> result = [];
        foreach (var block in blocks)
        {
            if (blockTypes.TryGetValue(block.BlockTypeId, out var bt))
            {
                result.Add(CmsProjectionMapper.ToResponse(block, bt));
            }
        }

        return Result.Success<IReadOnlyList<CmsBlockResponse>>(result);
    }
}
