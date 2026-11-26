using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class GetPersonalizationSegmentsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetPersonalizationSegmentsQuery, IReadOnlyList<PersonalizationSegmentResponse>>
{
    public async Task<Result<IReadOnlyList<PersonalizationSegmentResponse>>> HandleAsync(
        GetPersonalizationSegmentsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.PersonalizationSegments.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static s => s.IsActive);
        }

        var list = await q.OrderBy(static s => s.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<PersonalizationSegmentResponse>>([
            .. list.Select(CmsChannelProjectionMapper.ToResponse),
        ]);
    }
}
