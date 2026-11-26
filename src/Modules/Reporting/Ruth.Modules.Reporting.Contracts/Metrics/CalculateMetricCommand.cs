using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Executes a DataView-backed or SqlQuery-backed metric calculation and persists the new MetricValue.</summary>
/// <param name="MetricId">Metric identifier.</param>
/// <param name="AsOfUtc">Optional timestamp override.</param>
public sealed record CalculateMetricCommand(Guid MetricId, DateTimeOffset? AsOfUtc = null)
    : ICommand<MetricValueResponse>;
