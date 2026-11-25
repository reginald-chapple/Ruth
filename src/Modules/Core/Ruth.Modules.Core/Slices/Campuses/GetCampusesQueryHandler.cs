using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Campuses;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Campuses;

internal sealed class GetCampusesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetCampusesQuery, IReadOnlyList<CampusResponse>>
{
    public async Task<Result<IReadOnlyList<CampusResponse>>> HandleAsync(
        GetCampusesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext
            .Campuses.AsNoTracking()
            .Include(static c => c.Schedules)
            .Include(static c => c.Topics)
            .AsQueryable();

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(c =>
                c.NormalizedName.Contains(term)
                || (
                    c.ShortCode != null
                    && c.ShortCode.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                )
            );
        }

        var campuses = await baseQuery
            .OrderBy(static c => c.Order)
            .ThenBy(static c => c.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CampusResponse>>([
            .. campuses.Select(CampusMapper.ToResponse),
        ]);
    }
}
