using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class GetLocationTreeQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetLocationTreeQuery, IReadOnlyList<LocationTreeNodeResponse>>
{
    public async Task<Result<IReadOnlyList<LocationTreeNodeResponse>>> HandleAsync(
        GetLocationTreeQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.Locations.AsNoTracking().AsQueryable();
        if (query.NamedOnly)
        {
            baseQuery = baseQuery.Where(static l => l.Name != null && l.Name != string.Empty);
        }

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static l => l.IsActive);
        }

        var all = await baseQuery.OrderBy(static l => l.Name).ToListAsync(cancellationToken);
        var byParent = all.GroupBy(static l => l.ParentLocationId)
            .ToDictionary(
                static g => g.Key ?? LocationId.Empty,
                static g =>
                    g.OrderBy(static l => l.GetDisplayName(), StringComparer.OrdinalIgnoreCase)
                        .ToList()
            );

        var rootKey = query.RootLocationId.HasValue
            ? LocationId.From(query.RootLocationId.Value)
            : LocationId.Empty;

        var roots = BuildChildren(rootKey, byParent);
        return Result.Success<IReadOnlyList<LocationTreeNodeResponse>>(roots);
    }

    private static List<LocationTreeNodeResponse> BuildChildren(
        LocationId parentKey,
        Dictionary<LocationId, List<Location>> byParent
    )
    {
        if (!byParent.TryGetValue(parentKey, out var children))
        {
            return [];
        }

        var nodes = new List<LocationTreeNodeResponse>(children.Count);
        foreach (var child in children)
        {
            nodes.Add(
                new LocationTreeNodeResponse(
                    LocationMapper.ToResponse(child),
                    BuildChildren(child.Id, byParent)
                )
            );
        }

        return nodes;
    }
}
