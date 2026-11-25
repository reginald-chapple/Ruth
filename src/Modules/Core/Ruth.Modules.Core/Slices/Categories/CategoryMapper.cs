using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Slices.Categories;

internal static class CategoryMapper
{
    public static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse(
            category.Id.Value,
            category.SystemGuid,
            category.EntityTypeId.Value,
            category.EntityTypeQualifierColumn,
            category.EntityTypeQualifierValue,
            category.ParentCategoryId?.Value,
            category.Name,
            category.Description,
            category.IconName,
            category.HighlightColor,
            category.Order,
            category.IsSystem
        );
    }

    public static IReadOnlyList<CategoryTreeNodeResponse> BuildTree(
        IReadOnlyList<CategoryResponse> items
    )
    {
        var byParent = items
            .GroupBy(static c => c.ParentCategoryId ?? Guid.Empty)
            .ToDictionary(
                static g => g.Key,
                static g =>
                    g.OrderBy(static c => c.Order)
                        .ThenBy(static c => c.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList()
            );

        return BuildLevel(Guid.Empty, depth: 0, byParent, []);
    }

    private static List<CategoryTreeNodeResponse> BuildLevel(
        Guid parentId,
        int depth,
        Dictionary<Guid, List<CategoryResponse>> byParent,
        HashSet<Guid> visited
    )
    {
        if (depth > 32 || !byParent.TryGetValue(parentId, out var children))
        {
            return [];
        }

        var nodes = new List<CategoryTreeNodeResponse>(children.Count);
        foreach (var child in children)
        {
            if (!visited.Add(child.Id))
            {
                continue;
            }

            var subTree = BuildLevel(child.Id, depth + 1, byParent, visited);
            nodes.Add(
                new CategoryTreeNodeResponse(
                    child.Id,
                    child.SystemGuid,
                    child.EntityTypeId,
                    child.EntityTypeQualifierColumn,
                    child.EntityTypeQualifierValue,
                    child.ParentCategoryId,
                    child.Name,
                    child.Description,
                    child.IconName,
                    child.HighlightColor,
                    child.Order,
                    child.IsSystem,
                    depth,
                    subTree
                )
            );
        }

        return nodes;
    }
}
