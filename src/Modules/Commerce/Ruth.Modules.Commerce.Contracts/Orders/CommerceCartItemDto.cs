namespace Ruth.Modules.Commerce.Contracts.Orders;

/// <summary>Requested cart line item for pricing preview or order checkout.</summary>
/// <param name="VariantId">Target purchasable product variant identifier.</param>
/// <param name="Quantity">Positive integer quantity requested.</param>
public sealed record CommerceCartItemDto(Guid VariantId, int Quantity);
