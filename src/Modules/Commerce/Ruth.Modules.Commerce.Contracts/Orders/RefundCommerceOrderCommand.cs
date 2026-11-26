using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Processes a full or partial refund on a settled commerce order via Finance.Contracts.</summary>
/// <param name="OrderId">Target order identifier.</param>
/// <param name="Amount">Positive refund amount.</param>
/// <param name="Reason">Audit reason for refund.</param>
/// <param name="RestockInventory">When true on a full refund, restores tracked variant stock quantities.</param>
/// <param name="ProcessedByPersonId">Optional staff person identifier.</param>
public sealed record RefundCommerceOrderCommand(
    Guid OrderId,
    decimal Amount,
    string Reason,
    bool RestockInventory = true,
    Guid? ProcessedByPersonId = null
) : ICommand<CommerceOrderResponse>;
