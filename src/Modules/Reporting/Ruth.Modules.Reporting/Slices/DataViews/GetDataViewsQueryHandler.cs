using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.DataViews;

internal sealed class GetDataViewsQueryHandler(ReportingDbContext db)
    : IQueryHandler<GetDataViewsQuery, IReadOnlyList<DataViewResponse>>
{
    public async Task<Result<IReadOnlyList<DataViewResponse>>> HandleAsync(
        GetDataViewsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.DataViews.AsNoTracking().Include(static d => d.Filters).AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static d => d.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityTypeKey))
        {
            var key = query.EntityTypeKey.Trim();
            q = q.Where(d => d.EntityTypeKey == key);
        }

        var items = await q.OrderBy(static d => d.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DataViewResponse>>([
            .. items.Select(ReportingDtoMapper.ToResponse),
        ]);
    }
}
