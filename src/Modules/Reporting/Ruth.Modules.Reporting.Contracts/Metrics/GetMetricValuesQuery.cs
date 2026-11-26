using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Retrieves recorded MetricValues for a metric over an optional UTC date range.</summary>
/// <param name="MetricId">Target Metric identifier.</param>
/// <param name="StartDateUtc">Optional inclusive start UTC timestamp.</param>
/// <param name="EndDateUtc">Optional inclusive end UTC timestamp.</param>
/// <param name="PartitionEntityId">Optional partition entity filter.</param>
public sealed record GetMetricValuesQuery(
    Guid MetricId,
    DateTimeOffset? StartDateUtc = null,
    DateTimeOffset? EndDateUtc = null,
    Guid? PartitionEntityId = null
) : IQuery<IReadOnlyList<MetricValueResponse>>;
