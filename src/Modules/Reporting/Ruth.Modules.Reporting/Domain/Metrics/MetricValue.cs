using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Domain.Metrics;

/// <summary>Single observed Measure or Goal data point for a Metric with optional partition assignments.</summary>
public sealed class MetricValue : AggregateRoot<MetricValueId>, IAuditableEntity
{
    private readonly List<MetricValuePartition> _partitions = [];

    private MetricValue() { }

    private MetricValue(MetricValueId id)
        : base(id) { }

    public MetricId MetricId { get; private set; }
    public MetricValueType MetricValueType { get; private set; }
    public decimal YValue { get; private set; }
    public DateTimeOffset MetricValueDateTimeUtc { get; private set; }
    public string? XValue { get; private set; }
    public string? Note { get; private set; }

    public IReadOnlyCollection<MetricValuePartition> Partitions => _partitions.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public static Result<MetricValue> Create(
        Metric metric,
        MetricValueType valueType,
        decimal yValue,
        DateTimeOffset metricValueDateTimeUtc,
        string? xValue = null,
        string? note = null,
        IReadOnlyList<MetricPartitionValueInput>? partitions = null
    )
    {
        ArgumentNullException.ThrowIfNull(metric);

        var supplied = partitions ?? [];
        foreach (var required in metric.Partitions.Where(static p => p.IsRequired))
        {
            if (
                !supplied.Any(s =>
                    s.MetricPartitionId == required.Id.Value && s.EntityId is not null
                )
            )
            {
                return Result.Failure<MetricValue>(
                    ReportingErrors.Validation(
                        "MetricValue.PartitionRequired",
                        $"Required partition '{required.Label}' must supply an EntityId."
                    )
                );
            }
        }

        var entry = new MetricValue(MetricValueId.Create())
        {
            MetricId = metric.Id,
            MetricValueType = valueType,
            YValue = yValue,
            MetricValueDateTimeUtc = metricValueDateTimeUtc.ToUniversalTime(),
            XValue = string.IsNullOrWhiteSpace(xValue) ? null : xValue.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        };

        foreach (var part in supplied)
        {
            entry._partitions.Add(
                new MetricValuePartition(
                    entry.Id,
                    MetricPartitionId.From(part.MetricPartitionId),
                    part.EntityId,
                    part.EntityLabel
                )
            );
        }

        return Result.Success(entry);
    }
}
