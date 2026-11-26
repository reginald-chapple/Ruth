using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal sealed class GetEngagementOpportunitiesQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetEngagementOpportunitiesQuery, IReadOnlyList<EngagementOpportunityResponse>>
{
    public async Task<Result<IReadOnlyList<EngagementOpportunityResponse>>> HandleAsync(
        GetEngagementOpportunitiesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.EngagementOpportunities.AsNoTracking().AsQueryable();
        if (query.PipelineTypeId is { } pid && pid != Guid.Empty)
        {
            q = q.Where(o => o.PipelineTypeId == pid);
        }

        if (query.ActiveOnly)
        {
            q = q.Where(static o => o.IsActive);
        }

        if (query.PublicOnly)
        {
            q = q.Where(static o => o.IsActive && o.IsPublic);
        }

        var items = await q.OrderBy(static o => o.Name).ToListAsync(cancellationToken);
        var pipelines = await dbContext
            .PipelineTypes.AsNoTracking()
            .ToDictionaryAsync(static p => p.Id.Value, static p => p.Name, cancellationToken);

        var filtered = FilterByBranchAndKeyword(items, query.BranchId, query.SearchText);
        return filtered
            .Select(o =>
                PipelineProjectionMapper.ToResponse(
                    o,
                    pipelines.GetValueOrDefault(o.PipelineTypeId, "Pipeline")
                )
            )
            .ToArray();
    }

    private static IEnumerable<Domain.Pipelines.EngagementOpportunity> FilterByBranchAndKeyword(
        IEnumerable<Domain.Pipelines.EngagementOpportunity> source,
        Guid? branchId,
        string? searchText
    )
    {
        var result = source;
        if (branchId is { } bid && bid != Guid.Empty)
        {
            result = result.Where(o =>
                !o.DefaultBranchId.HasValue
                || o.DefaultBranchId == bid
                || o.BranchConfigs.Any(b => b.BranchId == bid)
            );
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            result = result.Where(o =>
                o.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (o.PublicSummary?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            );
        }

        return result;
    }
}
