using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Categories;

/// <summary>Creates a new category scoped to an entity type.</summary>
public sealed record CreateCategoryCommand(
    Guid EntityTypeId,
    string Name,
    Guid? ParentCategoryId = null,
    string? EntityTypeQualifierColumn = null,
    string? EntityTypeQualifierValue = null,
    string? Description = null,
    string? IconName = null,
    string? HighlightColor = null,
    int Order = 0
) : ICommand<CategoryResponse>;
