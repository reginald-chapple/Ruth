using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Previews storefront cart pricing, automatic member discounts, and IRS quid-pro-quo splits.</summary>
/// <param name="Items">Requested cart items.</param>
/// <param name="BuyerPersonId">Optional buyer CRM person identifier for automatic group/tier lookup.</param>
/// <param name="ExplicitMembershipTier">Optional explicit tier override when previewing.</param>
/// <param name="FulfillmentMethod">Requested fulfillment method.</param>
/// <param name="TaxRateOverridePercent">Optional jurisdiction/gateway tax rate override percent.</param>
public sealed record PreviewCommerceOrderQuoteQuery(
    IReadOnlyList<CommerceCartItemDto> Items,
    Guid? BuyerPersonId = null,
    string? ExplicitMembershipTier = null,
    CommerceFulfillmentMethod? FulfillmentMethod = null,
    decimal? TaxRateOverridePercent = null
) : IQuery<CommerceOrderQuoteResponse>;
