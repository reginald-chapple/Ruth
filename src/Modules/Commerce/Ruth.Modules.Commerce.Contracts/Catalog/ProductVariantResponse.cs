namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Read projection representing a product variant.</summary>
/// <param name="Id">Unique variant identifier.</param>
/// <param name="ProductId">Parent product identifier.</param>
/// <param name="Sku">Unique SKU code.</param>
/// <param name="Name">Variant display name.</param>
/// <param name="UnitPrice">List unit price.</param>
/// <param name="FairMarketValueAmount">IRS quid-pro-quo fair-market value per unit.</param>
/// <param name="EstimatedTaxDeductibleExcess">Estimated charitable excess at list price when quid-pro-quo is enabled.</param>
/// <param name="DigitalBinaryFileId">Optional digital Core BinaryFile identifier.</param>
/// <param name="PunchPassTypeId">Optional linked PunchPassType identifier.</param>
/// <param name="TrackInventory">Whether stock quantity is enforced.</param>
/// <param name="StockQuantity">Remaining stock units.</param>
/// <param name="DisplayOrder">Display sort order.</param>
/// <param name="IsActive">Whether the variant is active.</param>
public sealed record ProductVariantResponse(
    Guid Id,
    Guid ProductId,
    string Sku,
    string Name,
    decimal UnitPrice,
    decimal FairMarketValueAmount,
    decimal EstimatedTaxDeductibleExcess,
    Guid? DigitalBinaryFileId,
    Guid? PunchPassTypeId,
    bool TrackInventory,
    int StockQuantity,
    int DisplayOrder,
    bool IsActive
);
