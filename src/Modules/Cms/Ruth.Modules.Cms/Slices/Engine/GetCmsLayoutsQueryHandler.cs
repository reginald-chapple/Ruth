using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetCmsLayoutsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetCmsLayoutsQuery, IReadOnlyList<CmsLayoutResponse>>
{
    public async Task<Result<IReadOnlyList<CmsLayoutResponse>>> HandleAsync(
        GetCmsLayoutsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var siteId = CmsSiteId.From(query.SiteId);
        var list = await db
            .Layouts.AsNoTracking()
            .Where(l => l.SiteId == siteId)
            .OrderBy(static l => l.Name)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CmsLayoutResponse>>([
            .. list.Select(CmsProjectionMapper.ToResponse),
        ]);
    }
}
