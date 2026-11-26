using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Queries commerce orders filtered by buyer, status, or branch.</summary>
/// <param name="BuyerPersonId">Optional buyer person filter.</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="BranchId">Optional branch filter.</param>
public sealed record GetCommerceOrdersQuery(
    Guid? BuyerPersonId = null,
    CommerceOrderStatus? Status = null,
    Guid? BranchId = null
) : IQuery<IReadOnlyList<CommerceOrderResponse>>;
