using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Categories;

/// <summary>Deletes a non-system leaf category.</summary>
public sealed record DeleteCategoryCommand(Guid CategoryId) : ICommand<bool>;
