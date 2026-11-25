using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class GetSchedulesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetSchedulesQuery, IReadOnlyList<ScheduleResponse>>
{
    public async Task<Result<IReadOnlyList<ScheduleResponse>>> HandleAsync(
        GetSchedulesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.Schedules.AsNoTracking().AsQueryable();
        if (query.CategoryId.HasValue)
        {
            var catId = CategoryId.From(query.CategoryId.Value);
            baseQuery = baseQuery.Where(s => s.CategoryId == catId);
        }

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static s => s.IsActive);
        }

        var items = await baseQuery
            .OrderBy(static s => s.Order)
            .ThenBy(static s => s.Name)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            items =
            [
                .. items.Where(s =>
                    (
                        s.Name is not null
                        && s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        s.Description is not null
                        && s.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                    )
                ),
            ];
        }

        return Result.Success<IReadOnlyList<ScheduleResponse>>([
            .. items.Select(ScheduleMapper.ToResponse),
        ]);
    }
}
