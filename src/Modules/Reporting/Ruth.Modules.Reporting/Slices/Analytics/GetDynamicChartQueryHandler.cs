using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Analytics;

internal sealed class GetDynamicChartQueryHandler(ReportingDbContext db)
    : IQueryHandler<GetDynamicChartQuery, DynamicChartResponse>
{
    public async Task<Result<DynamicChartResponse>> HandleAsync(
        GetDynamicChartQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var metricId = MetricId.From(query.MetricId);
        var metric = await db
            .Metrics.AsNoTracking()
            .Include(static m => m.Partitions)
            .FirstOrDefaultAsync(m => m.Id == metricId, cancellationToken);
        if (metric is null)
        {
            return Result.Failure<DynamicChartResponse>(
                ReportingErrors.NotFound("Metric.NotFound", "Metric was not found.")
            );
        }

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

        var values = await q.OrderBy(static v => v.MetricValueDateTimeUtc)
            .ToListAsync(cancellationToken);
        var chart = MetricChartRollupCalculator.BuildChart(metric, values, query.Granularity);
        return Result.Success(chart);
    }
}
