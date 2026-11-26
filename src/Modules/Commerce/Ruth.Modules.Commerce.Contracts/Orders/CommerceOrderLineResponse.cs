using Ruth.Modules.Commerce.Contracts.Catalog;

namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Evaluated line item breakdown in a pricing quote or persisted commerce order.</summary>
/// <param name="LineId">Order line identifier (<see cref="Guid.Empty" /> on ephemeral quotes).</param>
/// <param name="ProductId">Catalog product identifier.</param>
/// <param name="VariantId">Catalog variant identifier.</param>
/// <param name="Sku">Variant SKU code.</param>
/// <param name="ProductName">Product title.</param>
/// <param name="VariantName">Variant label.</param>
/// <param name="ProductKind">Product fulfillment kind.</param>
/// <param name="Quantity">Units ordered.</param>
/// <param name="ListUnitPrice">Catalog list unit price before member discount.</param>
/// <param name="UnitMemberDiscountAmount">Automatic member discount deducted per unit.</param>
/// <param name="NetUnitPrice">Effective unit price after member discount.</param>
/// <param name="FairMarketValuePerUnit">IRS quid-pro-quo fair-market value per unit (0 when not quid-pro-quo).</param>
/// <param name="LineSubtotalAmount">Net line merchandise subtotal (NetUnitPrice * Quantity).</param>
/// <param name="LineTaxAmount">Sales tax computed strictly on the non-deductible commercial portion.</param>
/// <param name="LineShippingFeeAmount">Flat-rate shipping fee allocated to this line.</param>
/// <param name="NonDeductibleRevenueAmount">Amount routed to non-deductible RevenueAccountId (commercial value + tax + shipping).</param>
/// <param name="RevenueAccountId">Target non-deductible revenue FinancialAccount identifier.</param>
/// <param name="TaxDeductibleContributionAmount">IRS quid-pro-quo excess routed to tax-deductible CharitableAccountId.</param>
/// <param name="CharitableAccountId">Optional target tax-deductible CharitableFund FinancialAccount identifier.</param>
/// <param name="AppliedMemberPricingRuleId">Optional applied member pricing rule identifier.</param>
/// <param name="AppliedMemberPricingRuleName">Optional applied member pricing rule name.</param>
/// <param name="DigitalBinaryFileId">Optional Core BinaryFile identifier for digital download.</param>
/// <param name="IssuedPunchPassId">Optional IssuedPunchPass identifier created on settlement.</param>
public sealed record CommerceOrderLineResponse(
    Guid LineId,
    Guid ProductId,
    Guid VariantId,
    string Sku,
    string ProductName,
    string VariantName,
    ProductKind ProductKind,
    int Quantity,
    decimal ListUnitPrice,
    decimal UnitMemberDiscountAmount,
    decimal NetUnitPrice,
    decimal FairMarketValuePerUnit,
    decimal LineSubtotalAmount,
    decimal LineTaxAmount,
    decimal LineShippingFeeAmount,
    decimal NonDeductibleRevenueAmount,
    Guid RevenueAccountId,
    decimal TaxDeductibleContributionAmount,
    Guid? CharitableAccountId,
    Guid? AppliedMemberPricingRuleId,
    string? AppliedMemberPricingRuleName,
    Guid? DigitalBinaryFileId,
    Guid? IssuedPunchPassId
);
