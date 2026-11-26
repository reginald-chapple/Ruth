using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Reporting.Domain.Metrics;

/// <summary>Operational or executive Metric definition supporting manual entry, DataView, or SQL query sources.</summary>
public sealed class Metric
    : AggregateRoot<MetricId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Reporting.Metric";

    private readonly List<MetricPartition> _partitions = [];

    private Metric() { }

    private Metric(MetricId id)
        : base(id) { }

    public string Title { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public string MetricKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MetricCategoryId? CategoryId { get; private set; }
    public bool IsCumulative { get; private set; }
    public MetricSourceType SourceType { get; private set; }
    public DataViewId? DataViewId { get; private set; }
    public string? SourceQueryKey { get; private set; }
    public string? ScheduleCronExpression { get; private set; }
    public string? YAxisLabel { get; private set; }
    public string? UnitLabel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastRunDateTimeUtc { get; private set; }

    public IReadOnlyCollection<MetricPartition> Partitions => _partitions.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<Metric> Create(
        string title,
        string metricKey,
        MetricSourceType sourceType,
        string? subtitle = null,
        string? description = null,
        MetricCategoryId? categoryId = null,
        bool isCumulative = false,
        DataViewId? dataViewId = null,
        string? sourceQueryKey = null,
        string? scheduleCronExpression = null,
        string? yAxisLabel = null,
        string? unitLabel = null,
        IReadOnlyList<MetricPartitionSpec>? partitions = null
    )
    {
        var validation = ValidateInputs(title, metricKey, sourceType, dataViewId, sourceQueryKey);
        if (validation.IsFailure)
        {
            return Result.Failure<Metric>(validation.PrimaryError);
        }

        var metric = new Metric(MetricId.Create())
        {
            Title = title.Trim(),
            Subtitle = TrimOrNull(subtitle),
            MetricKey = ReportingTextNormalizer.NormalizeKey(metricKey),
            Description = TrimOrNull(description),
            CategoryId = categoryId,
            IsCumulative = isCumulative,
            SourceType = sourceType,
            DataViewId = dataViewId,
            SourceQueryKey = NormalizeOrNull(sourceQueryKey),
            ScheduleCronExpression = TrimOrNull(scheduleCronExpression),
            YAxisLabel = TrimOrNull(yAxisLabel),
            UnitLabel = TrimOrNull(unitLabel),
            IsActive = true,
        };

        var attachRes = metric.AttachPartitions(partitions);
        return attachRes.IsFailure
            ? Result.Failure<Metric>(attachRes.PrimaryError)
            : Result.Success(metric);
    }

    private Result AttachPartitions(IReadOnlyList<MetricPartitionSpec>? partitions)
    {
        if (partitions is not { Count: > 0 })
        {
            return Result.Success();
        }

        for (var i = 0; i < partitions.Count; i++)
        {
            var pRes = MetricPartition.Create(Id, partitions[i], i);
            if (pRes.IsFailure)
            {
                return Result.Failure(pRes.PrimaryError);
            }

            _partitions.Add(pRes.Value);
        }

        return Result.Success();
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : ReportingTextNormalizer.NormalizeKey(value);
    }

    public void RecordCalculationRun(DateTimeOffset runUtc)
    {
        LastRunDateTimeUtc = runUtc.ToUniversalTime();
    }

    private static Result ValidateInputs(
        string title,
        string metricKey,
        MetricSourceType sourceType,
        DataViewId? dataViewId,
        string? sourceQueryKey
    )
    {
        return string.IsNullOrWhiteSpace(title)
                ? Result.Failure(
                    ReportingErrors.Validation("Metric.TitleRequired", "Metric title is required.")
                )
            : string.IsNullOrWhiteSpace(ReportingTextNormalizer.NormalizeKey(metricKey))
                ? Result.Failure(
                    ReportingErrors.Validation("Metric.KeyInvalid", "Metric key is invalid.")
                )
            : sourceType == MetricSourceType.DataView
            && (dataViewId is null || dataViewId.Value.IsEmpty)
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "Metric.DataViewRequired",
                        "DataView-sourced metrics require a DataViewId."
                    )
                )
            : sourceType == MetricSourceType.SqlQuery && string.IsNullOrWhiteSpace(sourceQueryKey)
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "Metric.SourceQueryRequired",
                        "SqlQuery-sourced metrics require an allowlisted SourceQueryKey."
                    )
                )
            : Result.Success();
    }
}
