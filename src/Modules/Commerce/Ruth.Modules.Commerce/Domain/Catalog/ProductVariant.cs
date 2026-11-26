using System.Text.RegularExpressions;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Domain.Catalog;

/// <summary>Purchasable SKU variant belonging to a <see cref="Product" />.</summary>
public sealed partial class ProductVariant : Entity<ProductVariantId>
{
    private ProductVariant() { }

    internal ProductVariant(ProductVariantId id, ProductId productId)
        : base(id)
    {
        ProductId = productId;
    }

    public ProductId ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal FairMarketValueAmount { get; private set; }
    public Guid? DigitalBinaryFileId { get; private set; }
    public PunchPassTypeId? PunchPassTypeId { get; private set; }
    public bool TrackInventory { get; private set; }
    public int StockQuantity { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    internal Result ApplySpec(ProductKind parentKind, bool isQuidProQuo, ProductVariantSpec spec)
    {
        var normalizedSku = spec.Sku?.Trim().ToUpperInvariant() ?? string.Empty;
        if (
            !IsBaseSpecValid(spec, normalizedSku, isQuidProQuo)
            || !IsKindPayloadValid(parentKind, spec)
        )
        {
            return CommerceErrors.ProductInvalid;
        }

        AssignScalarFields(parentKind, isQuidProQuo, spec, normalizedSku);
        return Result.Success();
    }

    private void AssignScalarFields(
        ProductKind parentKind,
        bool isQuidProQuo,
        ProductVariantSpec spec,
        string normalizedSku
    )
    {
        Sku = normalizedSku;
        Name = spec.Name.Trim();
        UnitPrice = decimal.Round(spec.UnitPrice, 2, MidpointRounding.AwayFromZero);
        FairMarketValueAmount = isQuidProQuo
            ? decimal.Round(spec.FairMarketValueAmount, 2, MidpointRounding.AwayFromZero)
            : 0m;
        DigitalBinaryFileId =
            parentKind == ProductKind.DigitalDownload ? spec.DigitalBinaryFileId : null;
        PunchPassTypeId = ResolvePunchPassTypeId(parentKind, spec.PunchPassTypeId);
        TrackInventory = parentKind == ProductKind.PhysicalPickupOrFlatRate && spec.TrackInventory;
        StockQuantity = TrackInventory ? spec.StockQuantity : 0;
        DisplayOrder = spec.DisplayOrder;
        IsActive = spec.IsActive;
    }

    private static PunchPassTypeId? ResolvePunchPassTypeId(ProductKind kind, Guid? punchPassTypeId)
    {
        return kind == ProductKind.PunchPassPack && punchPassTypeId is { } ppId
            ? Domain.PunchPassTypeId.From(ppId)
            : null;
    }

    public Result DeductStock(int quantity)
    {
        if (quantity <= 0)
        {
            return CommerceErrors.OrderCartInvalid;
        }

        if (!TrackInventory)
        {
            return Result.Success();
        }

        if (StockQuantity < quantity)
        {
            return CommerceErrors.InsufficientStock;
        }

        StockQuantity -= quantity;
        return Result.Success();
    }

    public void Restock(int quantity)
    {
        if (TrackInventory && quantity > 0)
        {
            StockQuantity += quantity;
        }
    }

    private static bool IsBaseSpecValid(
        ProductVariantSpec spec,
        string normalizedSku,
        bool isQuidProQuo
    )
    {
        var roundedPrice = decimal.Round(spec.UnitPrice, 2, MidpointRounding.AwayFromZero);
        var roundedFmv = decimal.Round(
            spec.FairMarketValueAmount,
            2,
            MidpointRounding.AwayFromZero
        );
        var fmvValid = !isQuidProQuo || (roundedFmv >= 0m && roundedFmv <= roundedPrice);

        return SkuRegex.IsMatch(normalizedSku)
            && !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 160
            && roundedPrice >= 0m
            && fmvValid
            && (!spec.TrackInventory || spec.StockQuantity >= 0);
    }

    private static bool IsKindPayloadValid(ProductKind kind, ProductVariantSpec spec)
    {
        return kind switch
        {
            ProductKind.DigitalDownload => spec.DigitalBinaryFileId is { } f && f != Guid.Empty,
            ProductKind.PunchPassPack => spec.PunchPassTypeId is { } p && p != Guid.Empty,
            ProductKind.PhysicalPickupOrFlatRate => true,
            _ => false,
        };
    }

    [GeneratedRegex(
        @"^[A-Z0-9][A-Z0-9_-]{1,63}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SkuRegex { get; }
}
