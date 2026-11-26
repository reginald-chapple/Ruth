using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Lists configured Metrics optionally filtered by category.</summary>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="IncludeInactive">Whether to include inactive metrics.</param>
public sealed record GetMetricsQuery(Guid? CategoryId = null, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<MetricResponse>>;
