using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>Declarative DataView aggregate storing a Query AST filter tree and materialization metadata.</summary>
public sealed class DataView
    : AggregateRoot<DataViewId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Reporting.DataView";

    private readonly List<DataViewFilter> _filters = [];

    private DataView() { }

    private DataView(DataViewId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string EntityTypeKey { get; private set; } = string.Empty;
    public string? TransformKey { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsSystem { get; private set; }
    public int? PersistedScheduleIntervalMinutes { get; private set; }
    public DateTimeOffset? PersistedLastRefreshDateTimeUtc { get; private set; }
    public int? PersistedLastRunDurationMilliseconds { get; private set; }
    public int PersistedResultCount { get; private set; }

    public IReadOnlyCollection<DataViewFilter> Filters => _filters.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    string ISecured.EntityTypeKey => EntityTypeKeyName;
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

    public static Result<DataView> Create(
        string name,
        string entityTypeKey,
        DataViewFilterNodeDto rootFilter,
        string? description = null,
        Guid? categoryId = null,
        string? transformKey = null,
        int? persistedScheduleIntervalMinutes = null,
        bool isSystem = false
    )
    {
        var validation = ValidateDefinition(
            name,
            entityTypeKey,
            rootFilter,
            persistedScheduleIntervalMinutes
        );
        if (validation.IsFailure)
        {
            return Result.Failure<DataView>(validation.PrimaryError);
        }

        var dataView = new DataView(DataViewId.Create())
        {
            Name = name.Trim(),
            EntityTypeKey = entityTypeKey.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CategoryId = categoryId,
            TransformKey = string.IsNullOrWhiteSpace(transformKey) ? null : transformKey.Trim(),
            PersistedScheduleIntervalMinutes = persistedScheduleIntervalMinutes,
            IsActive = true,
            IsSystem = isSystem,
        };

        dataView.ReplaceFilterTreeInternal(rootFilter);
        return Result.Success(dataView);
    }

    public Result Update(
        string name,
        DataViewFilterNodeDto rootFilter,
        string? description,
        Guid? categoryId,
        string? transformKey,
        int? persistedScheduleIntervalMinutes,
        bool isActive
    )
    {
        var validation = ValidateDefinition(
            name,
            EntityTypeKey,
            rootFilter,
            persistedScheduleIntervalMinutes
        );
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CategoryId = categoryId;
        TransformKey = string.IsNullOrWhiteSpace(transformKey) ? null : transformKey.Trim();
        PersistedScheduleIntervalMinutes = persistedScheduleIntervalMinutes;
        IsActive = isActive;
        ReplaceFilterTreeInternal(rootFilter);
        return Result.Success();
    }

    public void RecordMaterialization(int resultCount, int durationMilliseconds, DateTimeOffset now)
    {
        PersistedResultCount = Math.Max(0, resultCount);
        PersistedLastRunDurationMilliseconds = Math.Max(0, durationMilliseconds);
        PersistedLastRefreshDateTimeUtc = now.ToUniversalTime();
    }

    public bool IsPersistedFreshAt(DateTimeOffset nowUtc)
    {
        return PersistedScheduleIntervalMinutes is > 0
            && PersistedLastRefreshDateTimeUtc is not null
            && PersistedLastRefreshDateTimeUtc.Value.AddMinutes(
                PersistedScheduleIntervalMinutes.Value
            ) > nowUtc;
    }

    public DataViewFilterNodeDto BuildFilterTree()
    {
        var root = _filters.Find(static f => f.ParentFilterId is null);
        if (root is null)
        {
            return new DataViewFilterNodeDto(DataViewFilterExpressionType.GroupAll);
        }

        var byParent = _filters
            .Where(static f => f.ParentFilterId is not null)
            .GroupBy(static f => f.ParentFilterId!.Value)
            .ToDictionary(static g => g.Key, static g => g.OrderBy(static x => x.Order).ToList());

        return MapFilterNode(root, byParent);
    }

    private void ReplaceFilterTreeInternal(DataViewFilterNodeDto rootFilter)
    {
        _filters.Clear();
        FlattenFilterNode(rootFilter, parentId: null, order: 0);
    }

    private void FlattenFilterNode(
        DataViewFilterNodeDto node,
        DataViewFilterId? parentId,
        int order
    )
    {
        var filter = DataViewFilter.Create(
            Id,
            parentId,
            node.ExpressionType,
            node.RuleKind,
            node.FieldKey,
            node.Operator,
            node.Value,
            node.SecondaryValue,
            order
        );
        _filters.Add(filter);

        if (node.Children is not { Count: > 0 })
        {
            return;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            FlattenFilterNode(node.Children[i], filter.Id, i);
        }
    }

    private static DataViewFilterNodeDto MapFilterNode(
        DataViewFilter filter,
        Dictionary<DataViewFilterId, List<DataViewFilter>> byParent
    )
    {
        IReadOnlyList<DataViewFilterNodeDto>? children = null;
        if (byParent.TryGetValue(filter.Id, out var childRows) && childRows.Count > 0)
        {
            children = [.. childRows.Select(c => MapFilterNode(c, byParent))];
        }

        return new DataViewFilterNodeDto(
            filter.ExpressionType,
            filter.RuleKind,
            filter.FieldKey,
            filter.Operator,
            filter.Value,
            filter.SecondaryValue,
            children
        );
    }

    private static Result ValidateDefinition(
        string name,
        string entityTypeKey,
        DataViewFilterNodeDto rootFilter,
        int? persistedScheduleIntervalMinutes
    )
    {
        return string.IsNullOrWhiteSpace(name)
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "DataView.NameRequired",
                        "DataView name is required."
                    )
                )
            : string.IsNullOrWhiteSpace(entityTypeKey)
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "DataView.EntityTypeRequired",
                        "DataView entity type key is required."
                    )
                )
            : persistedScheduleIntervalMinutes is <= 0
                ? Result.Failure(
                    ReportingErrors.Validation(
                        "DataView.IntervalInvalid",
                        "Persisted schedule interval must be greater than zero minutes."
                    )
                )
            : DataViewAstValidator.Validate(rootFilter);
    }
}
