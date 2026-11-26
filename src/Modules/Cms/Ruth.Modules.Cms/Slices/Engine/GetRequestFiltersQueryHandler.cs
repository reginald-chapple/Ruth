using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetRequestFiltersQueryHandler(CmsDbContext db)
    : IQueryHandler<GetRequestFiltersQuery, IReadOnlyList<RequestFilterResponse>>
{
    public async Task<Result<IReadOnlyList<RequestFilterResponse>>> HandleAsync(
        GetRequestFiltersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.RequestFilters.AsNoTracking();
        if (query.SiteId is { } sid)
        {
            var siteId = CmsSiteId.From(sid);
            q = q.Where(f => f.SiteId == null || f.SiteId == siteId);
        }

        if (!query.IncludeInactive)
        {
            q = q.Where(static f => f.IsActive);
        }

        var list = await q.OrderBy(static f => f.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<RequestFilterResponse>>([
            .. list.Select(CmsProjectionMapper.ToResponse),
        ]);
    }
}
