namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>DynamicChart payload for KPI and operational trend widgets.</summary>
/// <param name="MetricId">Backing Metric identifier.</param>
/// <param name="MetricTitle">Metric title.</param>
/// <param name="UnitLabel">Unit label (e.g., USD, People, %).</param>
/// <param name="Granularity">Time bucket granularity.</param>
/// <param name="LatestValue">Most recent observed measure value.</param>
/// <param name="TotalValue">Sum of measures in the selected window.</param>
/// <param name="AverageValue">Average measure per bucket.</param>
/// <param name="PeriodDeltaPercent">Percent change between first and last bucket.</param>
/// <param name="Series">Partitioned or combined time series.</param>
public sealed record DynamicChartResponse(
    Guid MetricId,
    string MetricTitle,
    string? UnitLabel,
    ChartRollupGranularity Granularity,
    decimal LatestValue,
    decimal TotalValue,
    decimal AverageValue,
    decimal? PeriodDeltaPercent,
    IReadOnlyList<DynamicChartSeriesDto> Series
);
