using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Categories;

internal sealed class CreateCategoryCommandHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : ICommandHandler<CreateCategoryCommand, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var entityTypeId = EntityTypeDescriptorId.From(command.EntityTypeId);
        var entityTypeExists = await dbContext.EntityTypeDescriptors.AnyAsync(
            e => e.Id == entityTypeId,
            cancellationToken
        );
        if (!entityTypeExists)
        {
            return CoreErrors.EntityTypeNotFound;
        }

        CategoryId? parentId = null;
        if (command.ParentCategoryId.HasValue && command.ParentCategoryId.Value != Guid.Empty)
        {
            var candidateParentId = CategoryId.From(command.ParentCategoryId.Value);
            var parent = await dbContext
                .Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == candidateParentId, cancellationToken);
            if (parent is null)
            {
                return CoreErrors.CategoryNotFound;
            }

            if (parent.EntityTypeId != entityTypeId)
            {
                return CoreErrors.CategoryEntityTypeMismatch;
            }

            parentId = candidateParentId;
        }

        var created = Category.Create(
            entityTypeId,
            command.Name,
            parentId,
            command.EntityTypeQualifierColumn,
            command.EntityTypeQualifierValue,
            command.Description,
            command.IconName,
            command.HighlightColor,
            command.Order
        );
        if (created.IsFailure)
        {
            return Result.Failure<CategoryResponse>(created.Errors);
        }

        _ = dbContext.Categories.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateBucketAsync(CoreReadCache.CategoriesBucket, cancellationToken);
        return CategoryMapper.ToResponse(created.Value);
    }
}
