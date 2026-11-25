namespace Ruth.Modules.Core.Contracts.Categories;

/// <summary>Recursive tree node projection of a <see cref="CategoryResponse" /> and its ordered child categories.</summary>
public sealed record CategoryTreeNodeResponse(
    Guid Id,
    Guid SystemGuid,
    Guid EntityTypeId,
    string? EntityTypeQualifierColumn,
    string? EntityTypeQualifierValue,
    Guid? ParentCategoryId,
    string Name,
    string? Description,
    string? IconName,
    string? HighlightColor,
    int Order,
    bool IsSystem,
    int Depth,
    IReadOnlyList<CategoryTreeNodeResponse> Children
);
