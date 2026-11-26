using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.Modules.Reporting.Infrastructure.Analytics;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Reporting.Slices.Metrics;

internal sealed class CalculateMetricCommandHandler(
    ReportingDbContext db,
    IQueryHandler<ExecuteDataViewQuery, DataViewExecutionResponse> executeDataView,
    ReportingCrossSchemaAnalyticsReader analyticsReader,
    IClock clock
) : ICommandHandler<CalculateMetricCommand, MetricValueResponse>
{
    public async Task<Result<MetricValueResponse>> HandleAsync(
        CalculateMetricCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = MetricId.From(command.MetricId);
        var metric = await db
            .Metrics.Include(static m => m.Partitions)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (metric is null)
        {
            return Result.Failure<MetricValueResponse>(
                ReportingErrors.NotFound("Metric.NotFound", "Metric was not found.")
            );
        }

        if (metric.SourceType == MetricSourceType.Manual)
        {
            return Result.Failure<MetricValueResponse>(
                ReportingErrors.Validation(
                    "Metric.ManualNotCalculable",
                    "Manual metrics must be entered via OperationalMetricsEntry."
                )
            );
        }

        var computed = await ComputeMetricValueAsync(metric, cancellationToken);
        if (computed.IsFailure)
        {
            return Result.Failure<MetricValueResponse>(computed.PrimaryError);
        }

        var asOf = (command.AsOfUtc ?? clock.UtcNow).ToUniversalTime();
        var valueRes = MetricValue.Create(
            metric,
            MetricValueType.Measure,
            computed.Value,
            asOf,
            note: $"Calculated from {metric.SourceType}"
        );
        if (valueRes.IsFailure)
        {
            return Result.Failure<MetricValueResponse>(valueRes.PrimaryError);
        }

        metric.RecordCalculationRun(asOf);
        _ = db.MetricValues.Add(valueRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(ReportingDtoMapper.ToResponse(valueRes.Value));
    }

    private async Task<Result<decimal>> ComputeMetricValueAsync(Metric metric, CancellationToken ct)
    {
        if (metric.SourceType == MetricSourceType.DataView && metric.DataViewId is { } dvId)
        {
            var dvRes = await executeDataView.HandleAsync(
                new ExecuteDataViewQuery(dvId.Value, MaxRows: 5000, UsePersistedIfFresh: false),
                ct
            );
            return dvRes.IsFailure
                ? Result.Failure<decimal>(dvRes.PrimaryError)
                : Result.Success((decimal)dvRes.Value.TotalMatchedCount);
        }

        var sqlVal = await analyticsReader.ExecuteAllowlistedSqlMetricAsync(
            metric.SourceQueryKey ?? string.Empty,
            ct
        );
        return Result.Success(sqlVal);
    }
}
