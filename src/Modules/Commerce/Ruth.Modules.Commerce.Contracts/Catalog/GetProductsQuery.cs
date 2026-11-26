using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Queries commerce catalog products.</summary>
/// <param name="ActiveOnly">When true, returns only active products.</param>
/// <param name="PublicOnly">When true, returns only public storefront products.</param>
/// <param name="Kind">Optional product kind filter.</param>
/// <param name="BranchId">Optional Branch/Site filter.</param>
public sealed record GetProductsQuery(
    bool ActiveOnly = false,
    bool PublicOnly = false,
    ProductKind? Kind = null,
    Guid? BranchId = null
) : IQuery<IReadOnlyList<ProductResponse>>;
