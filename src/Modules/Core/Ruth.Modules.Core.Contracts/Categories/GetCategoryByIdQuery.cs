using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Categories;

/// <summary>Retrieves a single category by identifier.</summary>
public sealed record GetCategoryByIdQuery(Guid CategoryId) : IQuery<CategoryResponse>;
