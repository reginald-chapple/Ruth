using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain;

/// <summary>Pure-domain tree invariant checker preventing cycles and cross-entity-type parenting in category hierarchies.</summary>
public static class CategoryTreeValidator
{
    /// <summary>Validates that moving <paramref name="categoryId" /> under <paramref name="newParentId" /> does not create a cycle.</summary>
    public static Result ValidateParentAssignment(
        CategoryId categoryId,
        EntityTypeDescriptorId categoryEntityTypeId,
        CategoryId? newParentId,
        IReadOnlyDictionary<
            CategoryId,
            (CategoryId? ParentId, EntityTypeDescriptorId EntityTypeId)
        > nodes
    )
    {
        ArgumentNullException.ThrowIfNull(nodes);

        if (!newParentId.HasValue)
        {
            return Result.Success();
        }

        if (newParentId.Value == categoryId)
        {
            return CoreErrors.CategoryCycleDetected;
        }

        if (!nodes.TryGetValue(newParentId.Value, out var parentInfo))
        {
            return CoreErrors.CategoryNotFound;
        }

        if (parentInfo.EntityTypeId != categoryEntityTypeId)
        {
            return CoreErrors.CategoryEntityTypeMismatch;
        }

        var visited = new HashSet<CategoryId> { categoryId };
        var cursor = newParentId;
        while (cursor.HasValue)
        {
            if (!visited.Add(cursor.Value))
            {
                return CoreErrors.CategoryCycleDetected;
            }

            cursor = nodes.TryGetValue(cursor.Value, out var info) ? info.ParentId : null;
        }

        return Result.Success();
    }
}
