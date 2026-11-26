using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Queries a DynamicChart rollup series for a Metric.</summary>
/// <param name="MetricId">Metric identifier.</param>
/// <param name="StartDateUtc">Optional inclusive start UTC date.</param>
/// <param name="EndDateUtc">Optional inclusive end UTC date.</param>
/// <param name="Granularity">Bucket granularity.</param>
/// <param name="PartitionEntityId">Optional partition filter.</param>
public sealed record GetDynamicChartQuery(
    Guid MetricId,
    DateTimeOffset? StartDateUtc = null,
    DateTimeOffset? EndDateUtc = null,
    ChartRollupGranularity Granularity = ChartRollupGranularity.Month,
    Guid? PartitionEntityId = null
) : IQuery<DynamicChartResponse>;
