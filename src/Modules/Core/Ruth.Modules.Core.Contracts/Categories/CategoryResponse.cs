namespace Ruth.Modules.Core.Contracts.Categories;

/// <summary>Represents a hierarchical category node scoped to an entity type.</summary>
public sealed record CategoryResponse(
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
    bool IsSystem
);
