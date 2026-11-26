using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class GetMobileApplicationsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetMobileApplicationsQuery, IReadOnlyList<MobileApplicationResponse>>
{
    public async Task<Result<IReadOnlyList<MobileApplicationResponse>>> HandleAsync(
        GetMobileApplicationsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db
            .MobileApplications.AsNoTracking()
            .Include(static a => a.Layouts)
            .Include(static a => a.Pages)
            .Include(static a => a.DeepLinks)
            .AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static a => a.IsActive);
        }

        var list = await q.OrderBy(static a => a.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MobileApplicationResponse>>([
            .. list.Select(CmsChannelProjectionMapper.ToResponse),
        ]);
    }
}
