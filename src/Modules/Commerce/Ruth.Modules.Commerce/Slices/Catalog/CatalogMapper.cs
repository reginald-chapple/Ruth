using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Domain.Pricing;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal static class CatalogMapper
{
    public static PunchPassTypeResponse MapPunchPassType(PunchPassType entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.Code,
            entity.Description,
            entity.CreditUnits,
            entity.ValidityDays,
            entity.AllowedBranchId,
            entity.AllowHouseholdSharing,
            entity.IsActive,
            entity.CreatedOnUtc
        );
    }

    public static ProductResponse MapProduct(Product product)
    {
        return new(
            product.Id.Value,
            product.Name,
            product.Slug,
            product.Summary,
            product.Description,
            product.Kind,
            product.CategoryId,
            product.BranchId,
            product.ImageBinaryFileId,
            product.CurrencyCode,
            product.RevenueAccountId,
            product.IsQuidProQuo,
            product.QuidProQuoCharitableAccountId,
            product.IsTaxable,
            product.FlatTaxRatePercent,
            product.AllowLocalPickup,
            product.AllowFlatRateShipping,
            product.FlatShippingFeeAmount,
            product.DefaultFinancialGatewayId,
            product.IsActive,
            product.IsPublic,
            [
                .. product
                    .Variants.OrderBy(static v => v.DisplayOrder)
                    .ThenBy(static v => v.Sku, StringComparer.Ordinal)
                    .Select(v => MapVariant(product, v)),
            ],
            new Dictionary<string, string?>(
                product.AttributeValues,
                StringComparer.OrdinalIgnoreCase
            ),
            product.CreatedOnUtc,
            product.ModifiedOnUtc
        );
    }

    public static MemberPricingRuleResponse MapRule(MemberPricingRule rule)
    {
        return new(
            rule.Id.Value,
            rule.Name,
            rule.Description,
            rule.DiscountKind,
            rule.DiscountValue,
            rule.RequiredMembershipTier,
            rule.RequiredGroupId,
            rule.RequiredGroupTypeId,
            rule.AppliesToProductId?.Value,
            rule.AppliesToProductKind,
            rule.Priority,
            rule.StartDate,
            rule.EndDate,
            rule.IsActive,
            rule.CreatedOnUtc
        );
    }

    private static ProductVariantResponse MapVariant(Product product, ProductVariant variant)
    {
        var excess = product.IsQuidProQuo
            ? Math.Max(0m, variant.UnitPrice - variant.FairMarketValueAmount)
            : 0m;
        return new ProductVariantResponse(
            variant.Id.Value,
            product.Id.Value,
            variant.Sku,
            variant.Name,
            variant.UnitPrice,
            variant.FairMarketValueAmount,
            excess,
            variant.DigitalBinaryFileId,
            variant.PunchPassTypeId?.Value,
            variant.TrackInventory,
            variant.StockQuantity,
            variant.DisplayOrder,
            variant.IsActive
        );
    }
}
