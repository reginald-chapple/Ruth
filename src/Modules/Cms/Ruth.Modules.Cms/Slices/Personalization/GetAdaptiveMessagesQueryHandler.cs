using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class GetAdaptiveMessagesQueryHandler(CmsDbContext db)
    : IQueryHandler<GetAdaptiveMessagesQuery, IReadOnlyList<AdaptiveMessageResponse>>
{
    public async Task<Result<IReadOnlyList<AdaptiveMessageResponse>>> HandleAsync(
        GetAdaptiveMessagesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.AdaptiveMessages.AsNoTracking().Include(static m => m.Adaptations).AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static m => m.IsActive);
        }

        var list = await q.OrderBy(static m => m.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<AdaptiveMessageResponse>>([
            .. list.Select(CmsChannelProjectionMapper.ToResponse),
        ]);
    }
}
