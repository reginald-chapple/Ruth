namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema Metric catalog dimension table (reporting.dim_metric).</summary>
public sealed class DimMetric
{
    public Guid MetricId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? UnitLabel { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public DateTimeOffset RefreshedAtUtc { get; set; }
}
