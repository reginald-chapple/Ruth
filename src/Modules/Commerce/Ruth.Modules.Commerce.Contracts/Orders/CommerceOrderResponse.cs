namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Read projection representing a persisted CommerceOrder.</summary>
/// <param name="Id">Unique order identifier.</param>
/// <param name="OrderNumber">Human-readable unique order number.</param>
/// <param name="Status">Current lifecycle status.</param>
/// <param name="BuyerPersonId">Optional buyer CRM person identifier.</param>
/// <param name="BuyerName">Buyer full name.</param>
/// <param name="BuyerEmail">Buyer contact email.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="FulfillmentMethod">Fulfillment method.</param>
/// <param name="ShippingAddressSummary">Optional shipping address summary.</param>
/// <param name="TrackingReference">Optional shipment or pickup locker reference.</param>
/// <param name="CurrencyCode">ISO-4217 currency code.</param>
/// <param name="SubtotalAmount">Net merchandise subtotal after member discounts.</param>
/// <param name="MemberDiscountTotalAmount">Total automatic member discounts applied.</param>
/// <param name="TaxTotalAmount">Total sales tax.</param>
/// <param name="ShippingFeeAmount">Total flat-rate shipping fee.</param>
/// <param name="TotalAmount">Grand total order amount.</param>
/// <param name="NonDeductibleRevenueAmount">Non-deductible operating revenue amount.</param>
/// <param name="TaxDeductibleContributionAmount">IRS quid-pro-quo tax-deductible contribution amount.</param>
/// <param name="PaidAmount">Amount settled to date.</param>
/// <param name="RefundedAmount">Amount refunded to date.</param>
/// <param name="LinkedFinancialTransactionId">Optional Finance transaction identifier.</param>
/// <param name="LinkedInvoiceId">Optional Finance invoice identifier.</param>
/// <param name="Lines">Ordered line items.</param>
/// <param name="IssuedPunchPassIds">Punch pass identifiers issued by this order.</param>
/// <param name="CreatedOnUtc">Order creation UTC timestamp.</param>
public sealed record CommerceOrderResponse(
    Guid Id,
    string OrderNumber,
    CommerceOrderStatus Status,
    Guid? BuyerPersonId,
    string BuyerName,
    string BuyerEmail,
    Guid? BranchId,
    CommerceFulfillmentMethod FulfillmentMethod,
    string? ShippingAddressSummary,
    string? TrackingReference,
    string CurrencyCode,
    decimal SubtotalAmount,
    decimal MemberDiscountTotalAmount,
    decimal TaxTotalAmount,
    decimal ShippingFeeAmount,
    decimal TotalAmount,
    decimal NonDeductibleRevenueAmount,
    decimal TaxDeductibleContributionAmount,
    decimal PaidAmount,
    decimal RefundedAmount,
    Guid? LinkedFinancialTransactionId,
    Guid? LinkedInvoiceId,
    IReadOnlyList<CommerceOrderLineResponse> Lines,
    IReadOnlyList<Guid> IssuedPunchPassIds,
    DateTimeOffset CreatedOnUtc
);
