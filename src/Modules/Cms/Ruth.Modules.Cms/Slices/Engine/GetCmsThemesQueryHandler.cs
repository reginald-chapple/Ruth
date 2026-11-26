using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetCmsThemesQueryHandler(CmsDbContext db)
    : IQueryHandler<GetCmsThemesQuery, IReadOnlyList<CmsThemeResponse>>
{
    public async Task<Result<IReadOnlyList<CmsThemeResponse>>> HandleAsync(
        GetCmsThemesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.Themes.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static t => t.IsActive);
        }

        var list = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CmsThemeResponse>>([
            .. list.Select(CmsProjectionMapper.ToResponse),
        ]);
    }
}
