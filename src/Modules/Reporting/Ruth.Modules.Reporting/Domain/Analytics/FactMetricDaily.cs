namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema daily operational metric fact table (reporting.fact_metric_daily).</summary>
public sealed class FactMetricDaily
{
    public Guid Id { get; set; }
    public int DateKey { get; set; }
    public Guid MetricId { get; set; }
    public Guid? PartitionEntityId { get; set; }
    public string PartitionLabel { get; set; } = "All";
    public decimal MeasureSum { get; set; }
    public decimal? GoalSum { get; set; }
    public int SampleCount { get; set; }
    public DateTimeOffset RefreshedAtUtc { get; set; }
}
