using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class CommerceCartEvaluationCollaborator(
    CommerceDbContext dbContext,
    MemberEligibilityResolver eligibilityResolver,
    ICommerceTaxCalculator taxCalculator
)
{
    public async Task<Result<EvaluatedCommerceOrderPlan>> EvaluateCartAsync(
        IReadOnlyList<CommerceCartItemDto> requestedItems,
        Guid? buyerPersonId,
        string? explicitMembershipTier,
        CommerceFulfillmentMethod? fulfillmentMethod,
        decimal? taxRateOverridePercent,
        bool trackCatalogEntities,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var variantIds = requestedItems
            .Where(static x => x.VariantId != Guid.Empty)
            .Select(static x => ProductVariantId.From(x.VariantId))
            .Distinct()
            .ToArray();

        IQueryable<Product> productsQuery = dbContext.Products.Include(static p => p.Variants);
        if (!trackCatalogEntities)
        {
            productsQuery = productsQuery.AsNoTracking();
        }

        var products = await productsQuery
            .Where(p => p.Variants.Any(v => variantIds.Contains(v.Id)))
            .ToListAsync(cancellationToken);

        var lookup = new Dictionary<Guid, (Product Product, ProductVariant Variant)>();
        foreach (var product in products)
        {
            foreach (var variant in product.Variants)
            {
                lookup[variant.Id.Value] = (product, variant);
            }
        }

        var rules = await dbContext
            .MemberPricingRules.AsNoTracking()
            .Where(static r => r.IsActive)
            .ToListAsync(cancellationToken);

        var profile = await eligibilityResolver.ResolveAsync(
            buyerPersonId,
            explicitMembershipTier,
            cancellationToken
        );

        return CommercePricingAndSplitEngine.Evaluate(
            requestedItems,
            lookup,
            rules,
            profile.Tiers,
            profile.GroupIds,
            profile.GroupTypeIds,
            fulfillmentMethod,
            taxCalculator,
            taxRateOverridePercent,
            DateOnly.FromDateTime(nowUtc.UtcDateTime)
        );
    }
}
