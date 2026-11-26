using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetCmsPagesQueryHandler(CmsDbContext db)
    : IQueryHandler<GetCmsPagesQuery, IReadOnlyList<CmsPageResponse>>
{
    public async Task<Result<IReadOnlyList<CmsPageResponse>>> HandleAsync(
        GetCmsPagesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var siteId = CmsSiteId.From(query.SiteId);
        var q = db
            .Pages.AsNoTracking()
            .Include(static p => p.Routes)
            .Include(static p => p.Contexts)
            .Where(p => p.SiteId == siteId);

        if (!query.IncludeUnpublished)
        {
            q = q.Where(static p => p.IsPublished);
        }

        var list = await q.OrderBy(static p => p.Order)
            .ThenBy(static p => p.InternalName)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CmsPageResponse>>([
            .. list.Select(CmsProjectionMapper.ToResponse),
        ]);
    }
}
