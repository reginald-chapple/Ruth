using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Gets a single commerce product by ID or slug.</summary>
/// <param name="IdOrSlug">Product GUID or kebab-case slug.</param>
public sealed record GetProductByIdOrSlugQuery(string IdOrSlug) : IQuery<ProductResponse>;
