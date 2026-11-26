using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetPageShortLinksQueryHandler(CmsDbContext db)
    : IQueryHandler<GetPageShortLinksQuery, IReadOnlyList<PageShortLinkResponse>>
{
    public async Task<Result<IReadOnlyList<PageShortLinkResponse>>> HandleAsync(
        GetPageShortLinksQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.PageShortLinks.AsNoTracking();
        if (query.SiteId is { } sid)
        {
            var siteId = CmsSiteId.From(sid);
            q = q.Where(l => l.SiteId == siteId);
        }

        var list = await q.OrderByDescending(static l => l.IsPinned)
            .ThenBy(static l => l.Token)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PageShortLinkResponse>>([
            .. list.Select(CmsProjectionMapper.ToResponse),
        ]);
    }
}
