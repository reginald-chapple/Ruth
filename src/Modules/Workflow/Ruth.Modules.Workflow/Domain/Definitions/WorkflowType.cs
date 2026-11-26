using System.Globalization;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Workflow.Domain.Definitions;

/// <summary>Organization-agnostic state/activity/action workflow definition aggregate.</summary>
public sealed class WorkflowType
    : AggregateRoot<WorkflowTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Workflow.WorkflowType";

    private readonly List<WorkflowActivityType> _activities = [];
    private List<WorkflowAttributeSpec> _declaredAttributes = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private WorkflowType() { }

    private WorkflowType(WorkflowTypeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string WorkTerm { get; private set; } = "Request";
    public string WorkflowIdPrefix { get; private set; } = "WF-";
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsPersisted { get; private set; } = true;
    public WorkflowLoggingLevel LoggingLevel { get; private set; } = WorkflowLoggingLevel.Action;
    public int ProcessingIntervalSeconds { get; private set; } = 60;
    public int MaxProcessingAttempts { get; private set; } = 5;
    public int NextSequenceNumber { get; private set; } = 1;

    public IReadOnlyList<WorkflowActivityType> Activities => _activities;

    public IReadOnlyList<WorkflowAttributeSpec> DeclaredAttributes
    {
        get => _declaredAttributes;
        private set => _declaredAttributes = [.. value];
    }

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

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

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    public static Result<WorkflowType> Create(WorkflowTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new WorkflowType(WorkflowTypeId.Create());
        var res = entity.Apply(spec);
        return res.IsFailure
            ? Result.Failure<WorkflowType>(res.PrimaryError)
            : Result.Success(entity);
    }

    public Result Update(WorkflowTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return Apply(spec);
    }

    public string AllocateNextWorkflowNumber()
    {
        var seq = NextSequenceNumber++;
        return string.Create(CultureInfo.InvariantCulture, $"{WorkflowIdPrefix}{seq:D5}");
    }

    public WorkflowActivityType? FindActivityByKey(string key)
    {
        return _activities.FirstOrDefault(a =>
            a.IsActive && string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase)
        );
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        var changed = false;
        foreach (var activity in _activities)
        {
            changed |= activity.RepointPerson(retiredPersonId, survivingPersonId);
        }

        return changed;
    }

    private Result Apply(WorkflowTypeSpec spec)
    {
        if (!TryValidateHeader(spec, out var slug))
        {
            return Result.Failure(WorkflowErrors.WorkflowTypeInvalid);
        }

        var attrsRes = ValidateAndNormalizeAttributes(spec.Attributes ?? []);
        if (attrsRes.IsFailure)
        {
            return attrsRes;
        }

        var actsRes = SyncActivities(spec.Activities);
        if (actsRes.IsFailure)
        {
            return actsRes;
        }

        if (!_activities.Exists(static a => a.IsActive && a.IsActivatedWithWorkflow))
        {
            return Result.Failure(WorkflowErrors.WorkflowTypeInvalid);
        }

        AssignHeaderProperties(spec, slug, attrsRes.Value);
        return Result.Success();
    }

    private static bool TryValidateHeader(WorkflowTypeSpec spec, out string slug)
    {
        slug = string.Empty;
        return !string.IsNullOrWhiteSpace(spec.Name)
            && !string.IsNullOrWhiteSpace(spec.WorkTerm)
            && !string.IsNullOrWhiteSpace(spec.WorkflowIdPrefix)
            && spec.ProcessingIntervalSeconds >= 1
            && spec.MaxProcessingAttempts >= 1
            && spec.Activities is { Count: > 0 }
            && WorkflowTextNormalizer.TryNormalizeSlug(spec.Slug, out slug);
    }

    private void AssignHeaderProperties(
        WorkflowTypeSpec spec,
        string slug,
        List<WorkflowAttributeSpec> attributes
    )
    {
        Name = spec.Name.Trim();
        Slug = slug;
        WorkTerm = spec.WorkTerm.Trim();
        WorkflowIdPrefix = spec.WorkflowIdPrefix.Trim();
        Description = WorkflowTextNormalizer.TrimOrNull(spec.Description);
        CategoryId = spec.CategoryId;
        IsActive = spec.IsActive;
        IsPersisted = spec.IsPersisted;
        LoggingLevel = spec.LoggingLevel;
        ProcessingIntervalSeconds = spec.ProcessingIntervalSeconds;
        MaxProcessingAttempts = spec.MaxProcessingAttempts;
        _declaredAttributes = attributes;
    }

    private static Result<List<WorkflowAttributeSpec>> ValidateAndNormalizeAttributes(
        IReadOnlyList<WorkflowAttributeSpec> specs
    )
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<WorkflowAttributeSpec>(specs.Count);
        foreach (var item in specs.OrderBy(static a => a.Order))
        {
            if (
                string.IsNullOrWhiteSpace(item.Key)
                || string.IsNullOrWhiteSpace(item.Name)
                || !seen.Add(item.Key.Trim())
            )
            {
                return Result.Failure<List<WorkflowAttributeSpec>>(
                    WorkflowErrors.WorkflowTypeInvalid
                );
            }

            normalized.Add(
                item with
                {
                    Key = item.Key.Trim(),
                    Name = item.Name.Trim(),
                    FieldTypeKey = string.IsNullOrWhiteSpace(item.FieldTypeKey)
                        ? "text"
                        : item.FieldTypeKey.Trim().ToLowerInvariant(),
                }
            );
        }

        return Result.Success(normalized);
    }

    private Result SyncActivities(IReadOnlyList<WorkflowActivityTypeSpec> specs)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updated = new List<WorkflowActivityType>(specs.Count);

        foreach (var item in specs.OrderBy(static a => a.Order))
        {
            if (
                !WorkflowTextNormalizer.TryNormalizeSlug(item.Key, out var normKey)
                || !seen.Add(normKey)
            )
            {
                return Result.Failure(WorkflowErrors.WorkflowTypeInvalid);
            }

            var existing = _activities.FirstOrDefault(a =>
                string.Equals(a.Key, normKey, StringComparison.OrdinalIgnoreCase)
            );
            if (existing is not null)
            {
                var res = existing.Apply(item);
                if (res.IsFailure)
                {
                    return res;
                }

                updated.Add(existing);
            }
            else
            {
                var created = WorkflowActivityType.Create(Id, item);
                if (created.IsFailure)
                {
                    return Result.Failure(created.PrimaryError);
                }

                updated.Add(created.Value);
            }
        }

        _activities.Clear();
        _activities.AddRange(updated);
        return Result.Success();
    }
}
