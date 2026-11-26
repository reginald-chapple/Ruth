namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Read projection representing a commerce catalog product and its variants.</summary>
/// <param name="Id">Unique product identifier.</param>
/// <param name="Name">Product display title.</param>
/// <param name="Slug">Unique kebab-case slug.</param>
/// <param name="Summary">Optional short summary.</param>
/// <param name="Description">Optional full description.</param>
/// <param name="Kind">Fulfillment and entitlement kind.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="ImageBinaryFileId">Optional image file identifier.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="RevenueAccountId">Non-deductible revenue FinancialAccount identifier.</param>
/// <param name="IsQuidProQuo">Whether IRS quid-pro-quo split accounting is enabled.</param>
/// <param name="QuidProQuoCharitableAccountId">Optional tax-deductible CharitableFund account identifier.</param>
/// <param name="IsTaxable">Whether commercial proceeds are taxable.</param>
/// <param name="FlatTaxRatePercent">Configured flat tax rate percent.</param>
/// <param name="AllowLocalPickup">Whether local pickup is enabled.</param>
/// <param name="AllowFlatRateShipping">Whether flat-rate shipping is enabled.</param>
/// <param name="FlatShippingFeeAmount">Flat shipping fee per unit.</param>
/// <param name="DefaultFinancialGatewayId">Optional default payment gateway identifier.</param>
/// <param name="IsActive">Whether the product is active.</param>
/// <param name="IsPublic">Whether the product is visible on the public storefront.</param>
/// <param name="Variants">Ordered product variants.</param>
/// <param name="Attributes">Extensible JSONB attributes.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
/// <param name="ModifiedOnUtc">Last modification UTC timestamp.</param>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Summary,
    string? Description,
    ProductKind Kind,
    Guid? CategoryId,
    Guid? BranchId,
    Guid? ImageBinaryFileId,
    string CurrencyCode,
    Guid RevenueAccountId,
    bool IsQuidProQuo,
    Guid? QuidProQuoCharitableAccountId,
    bool IsTaxable,
    decimal FlatTaxRatePercent,
    bool AllowLocalPickup,
    bool AllowFlatRateShipping,
    decimal FlatShippingFeeAmount,
    Guid? DefaultFinancialGatewayId,
    bool IsActive,
    bool IsPublic,
    IReadOnlyList<ProductVariantResponse> Variants,
    IReadOnlyDictionary<string, string?> Attributes,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
