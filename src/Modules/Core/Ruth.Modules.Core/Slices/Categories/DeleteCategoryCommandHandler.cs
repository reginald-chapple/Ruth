using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Categories;

internal sealed class DeleteCategoryCommandHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : ICommandHandler<DeleteCategoryCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = CategoryId.From(command.CategoryId);
        var entity = await dbContext.Categories.FirstOrDefaultAsync(
            c => c.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CoreErrors.CategoryNotFound;
        }

        if (entity.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        var hasChildren = await dbContext.Categories.AnyAsync(
            c => c.ParentCategoryId == id,
            cancellationToken
        );
        if (hasChildren)
        {
            return CoreErrors.CategoryHasChildren;
        }

        _ = dbContext.Categories.Remove(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.CategoriesBucket, cancellationToken);
        return true;
    }
}
