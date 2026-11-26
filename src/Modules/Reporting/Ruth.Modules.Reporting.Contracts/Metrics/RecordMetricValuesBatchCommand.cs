using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Records one or more operational metric values (manual OperationalMetricsEntry batch).</summary>
/// <param name="Entries">Batch of metric values to persist.</param>
public sealed record RecordMetricValuesBatchCommand(IReadOnlyList<MetricValueEntryInput> Entries)
    : ICommand<IReadOnlyList<MetricValueResponse>>;
