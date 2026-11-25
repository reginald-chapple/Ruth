using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Categories;

internal sealed class GetCategoryByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> HandleAsync(
        GetCategoryByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = CategoryId.From(query.CategoryId);
        var entity = await dbContext
            .Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is null ? CoreErrors.CategoryNotFound : CategoryMapper.ToResponse(entity);
    }
}
