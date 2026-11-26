using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class GetPublicSupportWallQueryHandler(EngagementDbContext dbContext, IClock clock)
    : IQueryHandler<GetPublicSupportWallQuery, IReadOnlyList<SupportRequestResponse>>
{
    public async Task<Result<IReadOnlyList<SupportRequestResponse>>> HandleAsync(
        GetPublicSupportWallQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var now = clock.UtcNow;
        var q = dbContext
            .SupportRequests.AsNoTracking()
            .Include(static r => r.Comments)
            .Where(r =>
                r.ModerationState == SupportModerationState.Approved
                && r.VisibilityLevel == SupportVisibilityLevel.PublicCommunityWall
                && r.ExpiresAtUtc > now
            );

        if (query.SupportCategoryId is { } cid && cid != Guid.Empty)
        {
            q = q.Where(r => r.SupportCategoryId == cid);
        }

        if (query.BranchId is { } bid && bid != Guid.Empty)
        {
            q = q.Where(r => r.BranchId == bid);
        }

        var items = await q.OrderByDescending(static r => r.EnteredAtUtc)
            .ToListAsync(cancellationToken);
        var categories = await dbContext
            .SupportCategories.AsNoTracking()
            .ToDictionaryAsync(static c => c.Id.Value, static c => c.Name, cancellationToken);

        return items
            .Select(r =>
                SupportProjectionMapper.ToResponse(
                    r,
                    categories.GetValueOrDefault(r.SupportCategoryId, "Support"),
                    redactPublicPii: true
                )
            )
            .ToArray();
    }
}
