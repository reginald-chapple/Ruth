using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class GetCareTeamReviewQueueQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetCareTeamReviewQueueQuery, IReadOnlyList<SupportRequestResponse>>
{
    public async Task<Result<IReadOnlyList<SupportRequestResponse>>> HandleAsync(
        GetCareTeamReviewQueueQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext
            .SupportRequests.AsNoTracking()
            .Include(static r => r.Comments)
            .AsQueryable();

        if (query.SupportCategoryId is { } cid && cid != Guid.Empty)
        {
            q = q.Where(r => r.SupportCategoryId == cid);
        }

        if (query.BranchId is { } bid && bid != Guid.Empty)
        {
            q = q.Where(r => r.BranchId == bid);
        }

        if (query.ModerationState.HasValue)
        {
            q = q.Where(r => r.ModerationState == query.ModerationState.Value);
        }
        else if (!query.IncludeArchived)
        {
            q = q.Where(static r => r.ModerationState != SupportModerationState.Archived);
        }

        var items = await q.ToListAsync(cancellationToken);
        var categories = await dbContext
            .SupportCategories.AsNoTracking()
            .ToDictionaryAsync(static c => c.Id.Value, static c => c.Name, cancellationToken);

        return items
            .OrderByDescending(static r => r.IsUrgent)
            .ThenBy(static r =>
                r.ModerationState switch
                {
                    SupportModerationState.Flagged => 0,
                    SupportModerationState.PendingReview => 1,
                    SupportModerationState.Approved => 2,
                    SupportModerationState.Archived => 3,
                    _ => 3,
                }
            )
            .ThenBy(static r => r.CommunitySupportCount)
            .ThenByDescending(static r => r.EnteredAtUtc)
            .Select(r =>
                SupportProjectionMapper.ToResponse(
                    r,
                    categories.GetValueOrDefault(r.SupportCategoryId, "Support"),
                    redactPublicPii: false
                )
            )
            .ToArray();
    }
}
