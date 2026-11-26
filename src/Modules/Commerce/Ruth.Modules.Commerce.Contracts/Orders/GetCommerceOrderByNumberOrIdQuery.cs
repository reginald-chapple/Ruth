using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Gets a single commerce order by order number or GUID.</summary>
/// <param name="OrderNumberOrId">Order number or GUID.</param>
public sealed record GetCommerceOrderByNumberOrIdQuery(string OrderNumberOrId)
    : IQuery<CommerceOrderResponse>;
