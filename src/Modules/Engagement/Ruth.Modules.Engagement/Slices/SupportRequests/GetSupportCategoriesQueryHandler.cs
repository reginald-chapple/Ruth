using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class GetSupportCategoriesQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetSupportCategoriesQuery, IReadOnlyList<SupportCategoryResponse>>
{
    public async Task<Result<IReadOnlyList<SupportCategoryResponse>>> HandleAsync(
        GetSupportCategoriesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.SupportCategories.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            q = q.Where(static c => c.IsActive);
        }

        var items = await q.OrderBy(static c => c.Name).ToListAsync(cancellationToken);
        return items.Select(SupportProjectionMapper.ToResponse).ToArray();
    }
}
