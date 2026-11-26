using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal sealed class UpdateProductCommandHandler(
    CommerceDbContext dbContext,
    ProductCatalogValidator validator
) : ICommandHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = ProductId.From(command.ProductId);
        var product = await dbContext
            .Products.Include(static p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return CommerceErrors.ProductNotFound;
        }

        var valid = await validator.ValidateAccountsAndUniquenessAsync(
            command.Spec,
            id,
            cancellationToken
        );
        if (valid.IsFailure)
        {
            return Result.Failure<ProductResponse>(valid.Errors);
        }

        var updated = product.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<ProductResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMapper.MapProduct(product);
    }
}
