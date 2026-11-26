using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Updates an existing commerce catalog product and reconciles its variants.</summary>
/// <param name="ProductId">Target product identifier.</param>
/// <param name="Spec">Updated product specification.</param>
public sealed record UpdateProductCommand(Guid ProductId, ProductSpec Spec)
    : ICommand<ProductResponse>;
