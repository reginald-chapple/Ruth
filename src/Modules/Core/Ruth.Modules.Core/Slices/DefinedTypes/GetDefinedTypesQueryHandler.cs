using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class GetDefinedTypesQueryHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : IQueryHandler<GetDefinedTypesQuery, IReadOnlyList<DefinedTypeSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<DefinedTypeSummaryResponse>>> HandleAsync(
        GetDefinedTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var all = await cache.GetOrLoadAsync(
            CoreReadCache.DefinedTypesBucket,
            async ct =>
                await dbContext
                    .DefinedTypes.AsNoTracking()
                    .OrderBy(static dt => dt.Order)
                    .ThenBy(static dt => dt.Name)
                    .Select(static dt => new DefinedTypeSummaryResponse(
                        dt.Id.Value,
                        dt.SystemGuid,
                        dt.Name,
                        dt.Description,
                        dt.HelpText,
                        dt.CategoryId.HasValue ? dt.CategoryId.Value.Value : null,
                        dt.Order,
                        dt.IsSystem,
                        dt.IsActive,
                        dt.EnableSecurityOnValues,
                        dt.CategorizedValuesEnabled,
                        dt.Values.Count
                    ))
                    .ToListAsync(ct),
            cancellationToken
        );

        IEnumerable<DefinedTypeSummaryResponse> filtered = all;
        if (query.CategoryId.HasValue)
        {
            filtered = filtered.Where(dt => dt.CategoryId == query.CategoryId.Value);
        }

        if (query.ActiveOnly)
        {
            filtered = filtered.Where(static dt => dt.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            filtered = filtered.Where(dt =>
                dt.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (
                    dt.Description is not null
                    && dt.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                )
            );
        }

        return Result.Success<IReadOnlyList<DefinedTypeSummaryResponse>>([.. filtered]);
    }
}
