using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetCmsSitesQueryHandler(CmsDbContext db)
    : IQueryHandler<GetCmsSitesQuery, IReadOnlyList<CmsSiteResponse>>
{
    public async Task<Result<IReadOnlyList<CmsSiteResponse>>> HandleAsync(
        GetCmsSitesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.Sites.AsNoTracking().Include(static s => s.Domains).AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static s => s.IsActive);
        }

        if (query.SiteType is { } st)
        {
            q = q.Where(s => s.SiteType == st);
        }

        var list = await q.OrderBy(static s => s.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CmsSiteResponse>>([
            .. list.Select(CmsProjectionMapper.ToResponse),
        ]);
    }
}
