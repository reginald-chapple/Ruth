using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Domain.Metrics;

/// <summary>Hierarchical category grouping operational and executive metrics.</summary>
public sealed class MetricCategory
    : AggregateRoot<MetricCategoryId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    public const string EntityTypeKeyName = "Ruth.Reporting.MetricCategory";

    private MetricCategory() { }

    private MetricCategory(MetricCategoryId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MetricCategoryId? ParentCategoryId { get; private set; }
    public int Order { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    public static Result<MetricCategory> Create(
        string name,
        string? description = null,
        MetricCategoryId? parentCategoryId = null,
        int order = 0
    )
    {
        return string.IsNullOrWhiteSpace(name)
            ? Result.Failure<MetricCategory>(
                ReportingErrors.Validation(
                    "MetricCategory.NameRequired",
                    "Metric category name is required."
                )
            )
            : Result.Success(
                new MetricCategory(MetricCategoryId.Create())
                {
                    Name = name.Trim(),
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    ParentCategoryId = parentCategoryId,
                    Order = order,
                }
            );
    }
}
