using System.Globalization;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Contracts.Metrics;

namespace Ruth.Modules.Reporting.Domain.Metrics;

/// <summary>Pure domain time-series aggregator computing DynamicChart buckets, rolling averages, and deltas.</summary>
public static class MetricChartRollupCalculator
{
    public static DynamicChartResponse BuildChart(
        Metric metric,
        IReadOnlyList<MetricValue> values,
        ChartRollupGranularity granularity
    )
    {
        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(values);

        var groupedBySeries = values
            .GroupBy(static v =>
            {
                var firstPartition = v.Partitions.FirstOrDefault();
                var label = firstPartition?.EntityLabel ?? "All";
                return (firstPartition?.EntityId, label);
            })
            .OrderBy(static g => g.Key.label, StringComparer.OrdinalIgnoreCase);

        List<DynamicChartSeriesDto> seriesList = [];
        foreach (var seriesGroup in groupedBySeries)
        {
            var points = BuildSeriesPoints(seriesGroup, granularity, metric.IsCumulative);
            seriesList.Add(
                new DynamicChartSeriesDto(seriesGroup.Key.label, seriesGroup.Key.EntityId, points)
            );
        }

        var combinedPoints = BuildSeriesPoints(values, granularity, metric.IsCumulative);
        var (latest, total, avg, periodDelta) = ComputeSummary(combinedPoints);

        return new DynamicChartResponse(
            metric.Id.Value,
            metric.Title,
            metric.UnitLabel,
            granularity,
            latest,
            total,
            avg,
            periodDelta,
            seriesList
        );
    }

    private static (
        decimal Latest,
        decimal Total,
        decimal Average,
        decimal? PeriodDelta
    ) ComputeSummary(List<DynamicChartPointDto> combinedPoints)
    {
        if (combinedPoints.Count == 0)
        {
            return (0m, 0m, 0m, null);
        }

        var latest = combinedPoints[^1].MeasureValue;
        var total = combinedPoints.Sum(static p => p.MeasureValue);
        var avg = Math.Round(total / combinedPoints.Count, 2, MidpointRounding.AwayFromZero);
        decimal? periodDelta =
            combinedPoints.Count >= 2 && combinedPoints[0].MeasureValue != 0m
                ? Math.Round(
                    (latest - combinedPoints[0].MeasureValue)
                        / Math.Abs(combinedPoints[0].MeasureValue)
                        * 100m,
                    2,
                    MidpointRounding.AwayFromZero
                )
                : null;
        return (latest, total, avg, periodDelta);
    }

    private static List<DynamicChartPointDto> BuildSeriesPoints(
        IEnumerable<MetricValue> values,
        ChartRollupGranularity granularity,
        bool isCumulative
    )
    {
        var buckets = values
            .GroupBy(v => TruncateToBucket(v.MetricValueDateTimeUtc, granularity))
            .OrderBy(static g => g.Key)
            .ToList();

        List<DynamicChartPointDto> points = [];
        var runningTotal = 0m;

        for (var i = 0; i < buckets.Count; i++)
        {
            var bucket = buckets[i];
            var measureRaw = bucket
                .Where(static v => v.MetricValueType == MetricValueType.Measure)
                .Sum(static v => v.YValue);
            runningTotal = isCumulative ? runningTotal + measureRaw : measureRaw;
            var measure = Math.Round(runningTotal, 2, MidpointRounding.AwayFromZero);

            var goals = bucket
                .Where(static v => v.MetricValueType == MetricValueType.Goal)
                .ToList();
            decimal? goal =
                goals.Count > 0
                    ? Math.Round(goals.Sum(static g => g.YValue), 2, MidpointRounding.AwayFromZero)
                    : null;

            decimal? delta = null;
            if (i > 0 && points[i - 1].MeasureValue != 0m)
            {
                var prev = points[i - 1].MeasureValue;
                delta = Math.Round(
                    (measure - prev) / Math.Abs(prev) * 100m,
                    2,
                    MidpointRounding.AwayFromZero
                );
            }

            var windowSum = measure;
            var windowStart = Math.Max(0, i - 2);
            for (var j = windowStart; j < i; j++)
            {
                windowSum += points[j].MeasureValue;
            }

            points.Add(
                new DynamicChartPointDto(
                    bucket.Key,
                    FormatBucketLabel(bucket.Key, granularity),
                    measure,
                    goal,
                    Math.Round(windowSum / (i - windowStart + 1), 2, MidpointRounding.AwayFromZero),
                    delta
                )
            );
        }

        return points;
    }

    public static DateTimeOffset TruncateToBucket(
        DateTimeOffset utc,
        ChartRollupGranularity granularity
    )
    {
        var d = utc.ToUniversalTime();
        return granularity switch
        {
            ChartRollupGranularity.Day => new DateTimeOffset(
                d.Year,
                d.Month,
                d.Day,
                0,
                0,
                0,
                TimeSpan.Zero
            ),
            ChartRollupGranularity.Week => TruncateToMonday(d),
            ChartRollupGranularity.Month => new DateTimeOffset(
                d.Year,
                d.Month,
                1,
                0,
                0,
                0,
                TimeSpan.Zero
            ),
            ChartRollupGranularity.Quarter => new DateTimeOffset(
                d.Year,
                ((d.Month - 1) / 3 * 3) + 1,
                1,
                0,
                0,
                0,
                TimeSpan.Zero
            ),
            _ => d,
        };
    }

    private static DateTimeOffset TruncateToMonday(DateTimeOffset d)
    {
        var diff = ((int)d.DayOfWeek + 6) % 7;
        var monday = d.AddDays(-diff);
        return new DateTimeOffset(monday.Year, monday.Month, monday.Day, 0, 0, 0, TimeSpan.Zero);
    }

    private static string FormatBucketLabel(
        DateTimeOffset bucketStart,
        ChartRollupGranularity granularity
    )
    {
        return granularity switch
        {
            ChartRollupGranularity.Day => bucketStart.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            ),
            ChartRollupGranularity.Week => string.Create(
                CultureInfo.InvariantCulture,
                $"Wk {bucketStart:yyyy-MM-dd}"
            ),
            ChartRollupGranularity.Month => bucketStart.ToString(
                "yyyy-MM",
                CultureInfo.InvariantCulture
            ),
            ChartRollupGranularity.Quarter => string.Create(
                CultureInfo.InvariantCulture,
                $"{bucketStart.Year}-Q{((bucketStart.Month - 1) / 3) + 1}"
            ),
            _ => bucketStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
    }
}
