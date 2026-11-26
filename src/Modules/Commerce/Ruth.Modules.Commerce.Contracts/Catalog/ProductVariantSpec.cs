namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Input specification for a purchasable SKU variant of a catalog product.</summary>
/// <param name="Sku">Unique normalized SKU code.</param>
/// <param name="Name">Variant display label (for example Standard, Large, Digital PDF).</param>
/// <param name="UnitPrice">Non-negative list unit price.</param>
/// <param name="FairMarketValueAmount">IRS quid-pro-quo fair-market value per unit (used when the parent product has IsQuidProQuo enabled).</param>
/// <param name="DigitalBinaryFileId">Required Core BinaryFile identifier when product kind is <see cref="ProductKind.DigitalDownload" />.</param>
/// <param name="PunchPassTypeId">Required PunchPassType identifier when product kind is <see cref="ProductKind.PunchPassPack" />.</param>
/// <param name="TrackInventory">Whether simple unit stock tracking is enforced for this variant.</param>
/// <param name="StockQuantity">Available stock units when <paramref name="TrackInventory" /> is true.</param>
/// <param name="DisplayOrder">Sort order within the product.</param>
/// <param name="IsActive">Whether this variant is available for sale.</param>
/// <param name="VariantId">Optional existing variant identifier when updating in place.</param>
public sealed record ProductVariantSpec(
    string Sku,
    string Name,
    decimal UnitPrice,
    decimal FairMarketValueAmount = 0m,
    Guid? DigitalBinaryFileId = null,
    Guid? PunchPassTypeId = null,
    bool TrackInventory = false,
    int StockQuantity = 0,
    int DisplayOrder = 0,
    bool IsActive = true,
    Guid? VariantId = null
);
