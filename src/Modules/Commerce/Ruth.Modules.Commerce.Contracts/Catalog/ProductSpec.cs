namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Input specification for creating or updating a commerce catalog product and its variants.</summary>
/// <param name="Name">Product display title.</param>
/// <param name="Slug">Unique URL-safe kebab-case slug.</param>
/// <param name="Kind">Fulfillment and entitlement kind.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="RevenueAccountId">Non-deductible operating or program fee FinancialAccount identifier.</param>
/// <param name="Variants">One or more purchasable SKU variants.</param>
/// <param name="Summary">Optional short storefront summary.</param>
/// <param name="Description">Optional detailed description.</param>
/// <param name="CategoryId">Optional Core Category identifier.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="ImageBinaryFileId">Optional product image Core BinaryFile identifier.</param>
/// <param name="IsQuidProQuo">When true, splits net proceeds into fair-market-value non-deductible revenue and excess tax-deductible charitable contribution.</param>
/// <param name="QuidProQuoCharitableAccountId">Required tax-deductible CharitableFund FinancialAccount identifier when <paramref name="IsQuidProQuo" /> is true.</param>
/// <param name="IsTaxable">Whether commercial proceeds for this product are subject to sales tax.</param>
/// <param name="FlatTaxRatePercent">Product flat tax percentage (0..100) used by default tax hook.</param>
/// <param name="AllowLocalPickup">Whether local pickup fulfillment is offered for physical items.</param>
/// <param name="AllowFlatRateShipping">Whether flat-rate shipping fulfillment is offered for physical items.</param>
/// <param name="FlatShippingFeeAmount">Flat shipping fee per unit when flat-rate shipping is selected.</param>
/// <param name="DefaultFinancialGatewayId">Optional default Finance payment gateway identifier.</param>
/// <param name="IsActive">Whether the product is active.</param>
/// <param name="IsPublic">Whether the product appears on the public storefront.</param>
/// <param name="Attributes">Optional extensible JSONB attribute key-value pairs.</param>
public sealed record ProductSpec(
    string Name,
    string Slug,
    ProductKind Kind,
    string CurrencyCode,
    Guid RevenueAccountId,
    IReadOnlyList<ProductVariantSpec> Variants,
    string? Summary = null,
    string? Description = null,
    Guid? CategoryId = null,
    Guid? BranchId = null,
    Guid? ImageBinaryFileId = null,
    bool IsQuidProQuo = false,
    Guid? QuidProQuoCharitableAccountId = null,
    bool IsTaxable = false,
    decimal FlatTaxRatePercent = 0m,
    bool AllowLocalPickup = true,
    bool AllowFlatRateShipping = false,
    decimal FlatShippingFeeAmount = 0m,
    Guid? DefaultFinancialGatewayId = null,
    bool IsActive = true,
    bool IsPublic = true,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
