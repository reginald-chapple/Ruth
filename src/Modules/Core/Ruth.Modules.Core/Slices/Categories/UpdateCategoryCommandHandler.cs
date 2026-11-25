using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Categories;

internal sealed class UpdateCategoryCommandHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : ICommandHandler<UpdateCategoryCommand, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> HandleAsync(
        UpdateCategoryCommand command,
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

        var updated = entity.Update(
            command.Name,
            command.Description,
            command.IconName,
            command.HighlightColor,
            command.Order
        );
        if (updated.IsFailure)
        {
            return Result.Failure<CategoryResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.CategoriesBucket, cancellationToken);
        return CategoryMapper.ToResponse(entity);
    }
}
