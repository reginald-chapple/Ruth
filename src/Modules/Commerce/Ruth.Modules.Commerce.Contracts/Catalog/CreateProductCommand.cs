using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Creates a new commerce catalog product and its variants.</summary>
/// <param name="Spec">Product specification.</param>
public sealed record CreateProductCommand(ProductSpec Spec) : ICommand<ProductResponse>;
