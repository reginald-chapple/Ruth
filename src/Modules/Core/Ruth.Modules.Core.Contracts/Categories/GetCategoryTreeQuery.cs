using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Categories;

/// <summary>Retrieves the hierarchical category tree for an entity type and optional qualifier.</summary>
public sealed record GetCategoryTreeQuery(
    Guid? EntityTypeId = null,
    string? EntityTypeKey = null,
    string? QualifierColumn = null,
    string? QualifierValue = null
) : IQuery<IReadOnlyList<CategoryTreeNodeResponse>>;
