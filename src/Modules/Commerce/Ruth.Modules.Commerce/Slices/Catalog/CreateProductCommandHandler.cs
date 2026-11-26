using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal sealed class CreateProductCommandHandler(
    CommerceDbContext dbContext,
    ProductCatalogValidator validator
) : ICommandHandler<CreateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = Product.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<ProductResponse>(created.Errors);
        }

        var valid = await validator.ValidateAccountsAndUniquenessAsync(
            command.Spec,
            currentProductId: null,
            cancellationToken
        );
        if (valid.IsFailure)
        {
            return Result.Failure<ProductResponse>(valid.Errors);
        }

        _ = dbContext.Products.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMapper.MapProduct(created.Value);
    }
}
