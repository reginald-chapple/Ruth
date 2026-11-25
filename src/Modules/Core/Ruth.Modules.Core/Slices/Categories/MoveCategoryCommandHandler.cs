using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Categories;

internal sealed class MoveCategoryCommandHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : ICommandHandler<MoveCategoryCommand, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> HandleAsync(
        MoveCategoryCommand command,
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

        CategoryId? newParentId =
            command.NewParentCategoryId.HasValue && command.NewParentCategoryId.Value != Guid.Empty
                ? CategoryId.From(command.NewParentCategoryId.Value)
                : null;

        var nodes = await dbContext
            .Categories.AsNoTracking()
            .ToDictionaryAsync(
                static c => c.Id,
                static c => (c.ParentCategoryId, c.EntityTypeId),
                cancellationToken
            );

        var cycleCheck = CategoryTreeValidator.ValidateParentAssignment(
            entity.Id,
            entity.EntityTypeId,
            newParentId,
            nodes
        );
        if (cycleCheck.IsFailure)
        {
            return Result.Failure<CategoryResponse>(cycleCheck.Errors);
        }

        var moveResult = entity.MoveTo(newParentId, command.NewOrder);
        if (moveResult.IsFailure)
        {
            return Result.Failure<CategoryResponse>(moveResult.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.CategoriesBucket, cancellationToken);
        return CategoryMapper.ToResponse(entity);
    }
}
