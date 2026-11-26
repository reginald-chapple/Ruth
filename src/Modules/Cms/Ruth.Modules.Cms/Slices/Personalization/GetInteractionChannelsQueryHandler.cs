using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class GetInteractionChannelsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetInteractionChannelsQuery, IReadOnlyList<InteractionChannelResponse>>
{
    public async Task<Result<IReadOnlyList<InteractionChannelResponse>>> HandleAsync(
        GetInteractionChannelsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.InteractionChannels.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static c => c.IsActive);
        }

        var channels = await q.OrderBy(static c => c.Name).ToListAsync(cancellationToken);
        var compCounts = await db
            .InteractionComponents.AsNoTracking()
            .GroupBy(static c => c.InteractionChannelId)
            .Select(static g => new { ChannelId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.ChannelId, static x => x.Count, cancellationToken);

        return Result.Success<IReadOnlyList<InteractionChannelResponse>>([
            .. channels.Select(c => new InteractionChannelResponse(
                c.Id.Value,
                c.Name,
                c.Key,
                c.ChannelType,
                c.RetentionDurationDays,
                c.IsActive,
                compCounts.GetValueOrDefault(c.Id, 0)
            )),
        ]);
    }
}
