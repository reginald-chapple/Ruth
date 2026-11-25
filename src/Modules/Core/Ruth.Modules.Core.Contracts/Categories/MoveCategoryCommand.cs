using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Categories;

/// <summary>Moves a category to a new parent (or root when <see langword="null" />) after cycle detection.</summary>
public sealed record MoveCategoryCommand(
    Guid CategoryId,
    Guid? NewParentCategoryId,
    int? NewOrder = null
) : ICommand<CategoryResponse>;
