using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Categories;

internal sealed class GetCategoryTreeQueryHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : IQueryHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryTreeNodeResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryTreeNodeResponse>>> HandleAsync(
        GetCategoryTreeQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var resolvedEntityTypeId = await ResolveEntityTypeIdAsync(query, cancellationToken);
        var all = await cache.GetOrLoadAsync(
            CoreReadCache.CategoriesBucket,
            async ct =>
                await dbContext
                    .Categories.AsNoTracking()
                    .OrderBy(static c => c.Order)
                    .ThenBy(static c => c.Name)
                    .Select(static c => new CategoryResponse(
                        c.Id.Value,
                        c.SystemGuid,
                        c.EntityTypeId.Value,
                        c.EntityTypeQualifierColumn,
                        c.EntityTypeQualifierValue,
                        c.ParentCategoryId.HasValue ? c.ParentCategoryId.Value.Value : null,
                        c.Name,
                        c.Description,
                        c.IconName,
                        c.HighlightColor,
                        c.Order,
                        c.IsSystem
                    ))
                    .ToListAsync(ct),
            cancellationToken
        );

        IEnumerable<CategoryResponse> filtered = all;
        if (resolvedEntityTypeId.HasValue)
        {
            filtered = filtered.Where(c => c.EntityTypeId == resolvedEntityTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.QualifierColumn))
        {
            var col = query.QualifierColumn.Trim();
            filtered = filtered.Where(c =>
                string.Equals(c.EntityTypeQualifierColumn, col, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (!string.IsNullOrWhiteSpace(query.QualifierValue))
        {
            var val = query.QualifierValue.Trim();
            filtered = filtered.Where(c =>
                string.Equals(c.EntityTypeQualifierValue, val, StringComparison.OrdinalIgnoreCase)
            );
        }

        return Result.Success(CategoryMapper.BuildTree([.. filtered]));
    }

    private async Task<Guid?> ResolveEntityTypeIdAsync(
        GetCategoryTreeQuery query,
        CancellationToken cancellationToken
    )
    {
        if (query.EntityTypeId.HasValue && query.EntityTypeId.Value != Guid.Empty)
        {
            return query.EntityTypeId.Value;
        }

        if (string.IsNullOrWhiteSpace(query.EntityTypeKey))
        {
            return null;
        }

        var key = query.EntityTypeKey.Trim();
        var match = await dbContext
            .EntityTypeDescriptors.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Key == key, cancellationToken);
        return match?.Id.Value;
    }
}
