using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Metrics;

internal sealed class GetMetricValuesQueryHandler(ReportingDbContext db)
    : IQueryHandler<GetMetricValuesQuery, IReadOnlyList<MetricValueResponse>>
{
    public async Task<Result<IReadOnlyList<MetricValueResponse>>> HandleAsync(
        GetMetricValuesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var metricId = MetricId.From(query.MetricId);
        var q = db
            .MetricValues.AsNoTracking()
            .Include(static v => v.Partitions)
            .Where(v => v.MetricId == metricId);

        if (query.StartDateUtc is { } start)
        {
            q = q.Where(v => v.MetricValueDateTimeUtc >= start.ToUniversalTime());
        }

        if (query.EndDateUtc is { } end)
        {
            q = q.Where(v => v.MetricValueDateTimeUtc <= end.ToUniversalTime());
        }

        if (query.PartitionEntityId is { } partId && partId != Guid.Empty)
        {
            q = q.Where(v => v.Partitions.Any(p => p.EntityId == partId));
        }

        var list = await q.OrderBy(static v => v.MetricValueDateTimeUtc)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MetricValueResponse>>([
            .. list.Select(ReportingDtoMapper.ToResponse),
        ]);
    }
}
