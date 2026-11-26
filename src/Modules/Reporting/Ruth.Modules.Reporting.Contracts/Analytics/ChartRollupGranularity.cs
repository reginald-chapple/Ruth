namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Time bucket rollup granularity for DynamicChart series.</summary>
public enum ChartRollupGranularity
{
    /// <summary>Daily aggregation buckets.</summary>
    Day = 0,

    /// <summary>Weekly aggregation buckets.</summary>
    Week = 1,

    /// <summary>Monthly aggregation buckets.</summary>
    Month = 2,

    /// <summary>Quarterly aggregation buckets.</summary>
    Quarter = 3,
}
