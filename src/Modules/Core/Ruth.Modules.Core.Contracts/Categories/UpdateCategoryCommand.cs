using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Categories;

/// <summary>Updates a category's properties.</summary>
public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    string? IconName,
    string? HighlightColor,
    int Order
) : ICommand<CategoryResponse>;
