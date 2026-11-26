using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal sealed class GetProductsQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Product> q = dbContext.Products.AsNoTracking().Include(static p => p.Variants);
        if (query.ActiveOnly)
        {
            q = q.Where(static p => p.IsActive);
        }

        if (query.PublicOnly)
        {
            q = q.Where(static p => p.IsPublic);
        }

        if (query.Kind.HasValue)
        {
            var kind = query.Kind.Value;
            q = q.Where(p => p.Kind == kind);
        }

        if (query.BranchId is { } b && b != Guid.Empty)
        {
            q = q.Where(p => p.BranchId == null || p.BranchId == b);
        }

        var products = await q.OrderBy(static p => p.Name).ToListAsync(cancellationToken);
        return products.Select(CatalogMapper.MapProduct).ToArray();
    }
}
