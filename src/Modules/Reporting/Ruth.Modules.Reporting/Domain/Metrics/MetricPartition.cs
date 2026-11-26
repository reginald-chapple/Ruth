using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Domain.Metrics;

/// <summary>Partition dimension on a Metric (e.g., Branch, Program, Fund).</summary>
public sealed class MetricPartition : Entity<MetricPartitionId>
{
    private MetricPartition() { }

    private MetricPartition(MetricPartitionId id)
        : base(id) { }

    public MetricId MetricId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string? EntityTypeKey { get; private set; }
    public bool IsRequired { get; private set; }
    public int Order { get; private set; }

    public static Result<MetricPartition> Create(
        MetricId metricId,
        MetricPartitionSpec spec,
        int order
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        return string.IsNullOrWhiteSpace(spec.Label)
            ? Result.Failure<MetricPartition>(
                ReportingErrors.Validation(
                    "MetricPartition.LabelRequired",
                    "Metric partition label is required."
                )
            )
            : Result.Success(
                new MetricPartition(MetricPartitionId.Create())
                {
                    MetricId = metricId,
                    Label = spec.Label.Trim(),
                    EntityTypeKey = string.IsNullOrWhiteSpace(spec.EntityTypeKey)
                        ? null
                        : spec.EntityTypeKey.Trim(),
                    IsRequired = spec.IsRequired,
                    Order = order,
                }
            );
    }
}
