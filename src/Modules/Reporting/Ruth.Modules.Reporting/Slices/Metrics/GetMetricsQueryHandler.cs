using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Metrics;

internal sealed class GetMetricsQueryHandler(ReportingDbContext db)
    : IQueryHandler<GetMetricsQuery, IReadOnlyList<MetricResponse>>
{
    public async Task<Result<IReadOnlyList<MetricResponse>>> HandleAsync(
        GetMetricsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.Metrics.AsNoTracking().Include(static m => m.Partitions).AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static m => m.IsActive);
        }

        if (query.CategoryId is { } cid && cid != Guid.Empty)
        {
            var catId = MetricCategoryId.From(cid);
            q = q.Where(m => m.CategoryId == catId);
        }

        var items = await q.OrderBy(static m => m.Title).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MetricResponse>>([
            .. items.Select(ReportingDtoMapper.ToResponse),
        ]);
    }
}
