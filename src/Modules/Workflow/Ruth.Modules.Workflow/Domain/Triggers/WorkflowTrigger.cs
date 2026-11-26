using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Workflow.Domain.Triggers;

/// <summary>Declarative automation trigger aggregate launching a WorkflowType when integration or entity stimuli match.</summary>
public sealed class WorkflowTrigger
    : AggregateRoot<WorkflowTriggerId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Workflow.WorkflowTrigger";

    private WorkflowTrigger() { }

    private WorkflowTrigger(WorkflowTriggerId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public WorkflowTriggerKind TriggerKind { get; private set; }
    public string EventTypeKey { get; private set; } = string.Empty;
    public WorkflowTypeId WorkflowTypeId { get; private set; }
    public string? EntityTypeQualifier { get; private set; }
    public string? FilterAttributeKey { get; private set; }
    public WorkflowTriggerOperator FilterOperator { get; private set; }
    public string? FilterExpectedValue { get; private set; }
    public string? WorkflowNameTemplate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int Order { get; private set; }

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

    public static Result<WorkflowTrigger> Create(WorkflowTriggerSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new WorkflowTrigger(WorkflowTriggerId.Create());
        var res = entity.Apply(spec);
        return res.IsFailure
            ? Result.Failure<WorkflowTrigger>(res.PrimaryError)
            : Result.Success(entity);
    }

    public Result Update(WorkflowTriggerSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return Apply(spec);
    }

    private Result Apply(WorkflowTriggerSpec spec)
    {
        if (
            string.IsNullOrWhiteSpace(spec.Name)
            || string.IsNullOrWhiteSpace(spec.EventTypeKey)
            || spec.WorkflowTypeId == Guid.Empty
        )
        {
            return Result.Failure(WorkflowErrors.TriggerInvalid);
        }

        if (
            spec.FilterOperator != WorkflowTriggerOperator.Always
            && string.IsNullOrWhiteSpace(spec.FilterAttributeKey)
        )
        {
            return Result.Failure(WorkflowErrors.TriggerInvalid);
        }

        Name = spec.Name.Trim();
        TriggerKind = spec.TriggerKind;
        EventTypeKey = spec.EventTypeKey.Trim().ToLowerInvariant();
        WorkflowTypeId = WorkflowTypeId.From(spec.WorkflowTypeId);
        EntityTypeQualifier = WorkflowTextNormalizer.TrimOrNull(spec.EntityTypeQualifier, 160);
        FilterAttributeKey = WorkflowTextNormalizer.TrimOrNull(spec.FilterAttributeKey, 120);
        FilterOperator = spec.FilterOperator;
        FilterExpectedValue = WorkflowTextNormalizer.TrimOrNull(spec.FilterExpectedValue, 400);
        WorkflowNameTemplate = WorkflowTextNormalizer.TrimOrNull(spec.WorkflowNameTemplate, 250);
        IsActive = spec.IsActive;
        Order = spec.Order;
        return Result.Success();
    }
}
