using System.Globalization;
using Ruth.Modules.Commerce.Contracts.Catalog;

namespace Ruth.Host.Components.Pages.Commerce;

internal static class CommerceCatalogSpecFactory
{
    public static bool TryBuildProductSpec(
        string productName,
        string productSlug,
        ProductKind kind,
        Guid revenueAccountId,
        Guid charitableAccountId,
        Guid linkedPunchPassTypeId,
        string sku,
        string variantName,
        string unitPriceInput,
        string fmvInput,
        string taxRateInput,
        string stockInput,
        out ProductSpec? spec
    )
    {
        spec = null;
        if (!TryParseNumbers(unitPriceInput, fmvInput, taxRateInput, stockInput, out var parsed))
        {
            return false;
        }

        var isQuidProQuo = parsed.Fmv > 0m && charitableAccountId != Guid.Empty;
        spec = new ProductSpec(
            productName,
            productSlug,
            kind,
            "USD",
            revenueAccountId,
            [
                new ProductVariantSpec(
                    sku,
                    variantName,
                    parsed.Price,
                    parsed.Fmv,
                    PunchPassTypeId: kind == ProductKind.PunchPassPack
                        ? linkedPunchPassTypeId
                        : null,
                    TrackInventory: kind == ProductKind.PhysicalPickupOrFlatRate,
                    StockQuantity: parsed.Stock
                ),
            ],
            IsQuidProQuo: isQuidProQuo,
            QuidProQuoCharitableAccountId: isQuidProQuo ? charitableAccountId : null,
            IsTaxable: parsed.Tax > 0m,
            FlatTaxRatePercent: parsed.Tax,
            AllowLocalPickup: true,
            AllowFlatRateShipping: true,
            FlatShippingFeeAmount: 10.00m
        );
        return true;
    }

    private static bool TryParseNumbers(
        string priceText,
        string fmvText,
        string taxText,
        string stockText,
        out (decimal Price, decimal Fmv, decimal Tax, int Stock) values
    )
    {
        values = default;
        if (
            !decimal.TryParse(
                priceText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var p
            )
            || !decimal.TryParse(
                fmvText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var f
            )
            || !decimal.TryParse(
                taxText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var t
            )
            || !int.TryParse(
                stockText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var s
            )
        )
        {
            return false;
        }

        values = (p, f, t, s);
        return true;
    }
}
