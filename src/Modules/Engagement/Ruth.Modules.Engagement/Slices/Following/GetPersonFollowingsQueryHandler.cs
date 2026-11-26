using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Following;

internal sealed class GetPersonFollowingsQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetPersonFollowingsQuery, IReadOnlyList<FollowingSubscriptionResponse>>
{
    public async Task<Result<IReadOnlyList<FollowingSubscriptionResponse>>> HandleAsync(
        GetPersonFollowingsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var items = await dbContext
            .FollowingSubscriptions.AsNoTracking()
            .Where(f => f.FollowerPersonId == query.FollowerPersonId && f.IsSubscribed)
            .OrderBy(static f => f.EntityDisplayName)
            .ToListAsync(cancellationToken);

        return items.Select(ToggleFollowingSubscriptionCommandHandler.ToResponse).ToArray();
    }
}
