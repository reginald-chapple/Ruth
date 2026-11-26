namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Storefront pricing, member discount, tax, shipping, and IRS quid-pro-quo split quote.</summary>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="FulfillmentMethod">Evaluated fulfillment method.</param>
/// <param name="ListSubtotalAmount">Gross list price total before member discounts.</param>
/// <param name="MemberDiscountTotalAmount">Total automatic member pricing savings.</param>
/// <param name="NetSubtotalAmount">Net item subtotal after member discounts.</param>
/// <param name="TaxTotalAmount">Total sales tax on taxable commercial amounts.</param>
/// <param name="ShippingFeeAmount">Total flat-rate shipping fee.</param>
/// <param name="GrandTotalAmount">Total payable amount (NetSubtotal + Tax + Shipping).</param>
/// <param name="NonDeductibleRevenueAmount">Total non-deductible commercial portion (FMV/merchandise + tax + shipping).</param>
/// <param name="TaxDeductibleContributionAmount">Total IRS quid-pro-quo tax-deductible charitable contribution portion.</param>
/// <param name="HasQuidProQuoDisclosure">True when IRS Publication 1771 quid-pro-quo disclosure applies to this order.</param>
/// <param name="Lines">Detailed per-item breakdown.</param>
/// <param name="AccountSplits">Aggregated Finance account splits.</param>
public sealed record CommerceOrderQuoteResponse(
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
    IReadOnlyList<CommerceOrderLineResponse> Lines,
    IReadOnlyList<CommerceLedgerSplitPreviewDto> AccountSplits
);
