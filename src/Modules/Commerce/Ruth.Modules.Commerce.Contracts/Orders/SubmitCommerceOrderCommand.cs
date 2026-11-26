using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Submits a storefront or staff commerce order, settles splits or invoice via Finance, decrements inventory, and issues punch passes.</summary>
/// <param name="BuyerName">Buyer full name.</param>
/// <param name="BuyerEmail">Buyer contact email.</param>
/// <param name="Items">One or more cart items.</param>
/// <param name="BuyerPersonId">Optional buyer CRM person identifier.</param>
/// <param name="ExplicitMembershipTier">Optional explicit membership tier when not resolved from Wallet Passes.</param>
/// <param name="BranchId">Optional Branch/Site identifier.</param>
/// <param name="FulfillmentMethod">Requested fulfillment method.</param>
/// <param name="ShippingAddressSummary">Required mailing address summary when <see cref="CommerceFulfillmentMethod.FlatRateShipping" /> is chosen.</param>
/// <param name="FinancialGatewayId">Optional gateway identifier override for electronic card/token charge.</param>
/// <param name="OneTimeHostedPaymentToken">Optional single-use hosted payment token.</param>
/// <param name="SavedPaymentMethodId">Optional vaulted payment method identifier.</param>
/// <param name="ExternalTransactionCode">Optional check/cash reference for staff counter tender.</param>
/// <param name="RequestInvoiceBilling">When true, issues an open Finance Invoice instead of charging immediately.</param>
/// <param name="TaxRateOverridePercent">Optional tax rate override percentage.</param>
public sealed record SubmitCommerceOrderCommand(
    string BuyerName,
    string BuyerEmail,
    IReadOnlyList<CommerceCartItemDto> Items,
    Guid? BuyerPersonId = null,
    string? ExplicitMembershipTier = null,
    Guid? BranchId = null,
    CommerceFulfillmentMethod? FulfillmentMethod = null,
    string? ShippingAddressSummary = null,
    Guid? FinancialGatewayId = null,
    string? OneTimeHostedPaymentToken = null,
    Guid? SavedPaymentMethodId = null,
    string? ExternalTransactionCode = null,
    bool RequestInvoiceBilling = false,
    decimal? TaxRateOverridePercent = null
) : ICommand<CommerceOrderResponse>;
