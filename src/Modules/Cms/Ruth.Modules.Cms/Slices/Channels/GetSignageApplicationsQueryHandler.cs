using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class GetSignageApplicationsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetSignageApplicationsQuery, IReadOnlyList<SignageApplicationResponse>>
{
    public async Task<Result<IReadOnlyList<SignageApplicationResponse>>> HandleAsync(
        GetSignageApplicationsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.SignageApplications.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static a => a.IsActive);
        }

        if (query.Platform is { } p)
        {
            q = q.Where(a => a.Platform == p);
        }

        var list = await q.OrderBy(static a => a.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SignageApplicationResponse>>([
            .. list.Select(CmsChannelProjectionMapper.ToResponse),
        ]);
    }
}
