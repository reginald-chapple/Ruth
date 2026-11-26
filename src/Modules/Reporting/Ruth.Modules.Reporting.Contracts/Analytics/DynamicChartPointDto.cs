namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Single time-bucket point in a DynamicChart series.</summary>
/// <param name="BucketStartUtc">Bucket start timestamp in UTC.</param>
/// <param name="Label">Formatted X-axis bucket label.</param>
/// <param name="MeasureValue">Aggregated observed measure.</param>
/// <param name="GoalValue">Optional aggregated goal value.</param>
/// <param name="RollingAverage">Trailing 3-bucket moving average.</param>
/// <param name="DeltaPercent">Percent change relative to previous bucket.</param>
public sealed record DynamicChartPointDto(
    DateTimeOffset BucketStartUtc,
    string Label,
    decimal MeasureValue,
    decimal? GoalValue,
    decimal? RollingAverage,
    decimal? DeltaPercent
);
