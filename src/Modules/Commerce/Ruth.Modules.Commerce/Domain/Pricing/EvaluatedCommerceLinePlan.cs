using Ruth.Modules.Commerce.Domain.Catalog;

namespace Ruth.Modules.Commerce.Domain.Pricing;

/// <summary>Evaluated order line plan produced by <see cref="CommercePricingAndSplitEngine" />.</summary>
public sealed record EvaluatedCommerceLinePlan(
    Product Product,
    ProductVariant Variant,
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
    MemberPricingRuleId? AppliedRuleId,
    string? AppliedRuleName
);
