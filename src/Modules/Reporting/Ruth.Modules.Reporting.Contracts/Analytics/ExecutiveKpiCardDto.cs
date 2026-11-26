namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Executive KPI card with period-over-period delta and sparkline values.</summary>
/// <param name="Key">Stable KPI key.</param>
/// <param name="Title">Display title.</param>
/// <param name="FormattedValue">Formatted primary KPI value.</param>
/// <param name="NumericValue">Raw numeric KPI value.</param>
/// <param name="Unit">Unit label.</param>
/// <param name="DeltaPercent">Percent delta compared to previous period.</param>
/// <param name="Sparkline">Recent trend points.</param>
public sealed record ExecutiveKpiCardDto(
    string Key,
    string Title,
    string FormattedValue,
    decimal NumericValue,
    string Unit,
    decimal? DeltaPercent,
    IReadOnlyList<decimal> Sparkline
);
