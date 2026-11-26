using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal sealed class GetProductByIdOrSlugQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetProductByIdOrSlugQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(
        GetProductByIdOrSlugQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.IdOrSlug))
        {
            return CommerceErrors.ProductNotFound;
        }

        var raw = query.IdOrSlug.Trim();
        var lowerSlug = raw.ToLowerInvariant();
        var q = dbContext.Products.AsNoTracking().Include(static p => p.Variants);
        var product = Guid.TryParse(raw, out var guid)
            ? await q.FirstOrDefaultAsync(p => p.Id == ProductId.From(guid), cancellationToken)
            : await q.FirstOrDefaultAsync(p => p.Slug == lowerSlug, cancellationToken);

        return product is null ? CommerceErrors.ProductNotFound : CatalogMapper.MapProduct(product);
    }
}
