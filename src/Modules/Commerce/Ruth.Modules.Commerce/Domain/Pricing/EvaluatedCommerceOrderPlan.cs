using Ruth.Modules.Commerce.Contracts.Orders;

namespace Ruth.Modules.Commerce.Domain.Pricing;

/// <summary>Evaluated cart plan including IRS Publication 1771 quid-pro-quo splits.</summary>
public sealed record EvaluatedCommerceOrderPlan(
    string CurrencyCode,
    CommerceFulfillmentMethod FulfillmentMethod,
    decimal ListSubtotalAmount,
    decimal MemberDiscountTotalAmount,
    decimal NetSubtotalAmount,
    decimal TaxTotalAmount,
    decimal ShippingFeeAmount,
    decimal GrandTotalAmount,
    decimal NonDeductibleRevenueAmount,
    decimal TaxDeductibleContributionAmount,
    bool HasQuidProQuoDisclosure,
    IReadOnlyList<EvaluatedCommerceLinePlan> Lines,
    IReadOnlyList<CommerceLedgerSplitPreviewDto> AccountSplits
);
