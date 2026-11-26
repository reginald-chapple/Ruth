using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Advances the fulfillment status of a paid physical commerce order.</summary>
/// <param name="OrderId">Target order identifier.</param>
/// <param name="NewStatus">Target fulfillment status (ReadyForPickup, Shipped, or Fulfilled).</param>
/// <param name="TrackingReference">Optional carrier tracking reference or pickup note.</param>
public sealed record UpdateCommerceOrderFulfillmentCommand(
    Guid OrderId,
    CommerceOrderStatus NewStatus,
    string? TrackingReference = null
) : ICommand<CommerceOrderResponse>;
