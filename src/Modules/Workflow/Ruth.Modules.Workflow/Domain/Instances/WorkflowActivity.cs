using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain.Instances;

/// <summary>Runtime activity stage instance owned by a WorkflowInstance.</summary>
public sealed class WorkflowActivity : Entity<WorkflowActivityId>
{
    private readonly List<WorkflowAction> _actions = [];
    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private WorkflowActivity() { }

    private WorkflowActivity(WorkflowActivityId id)
        : base(id) { }

    public WorkflowId WorkflowId { get; private set; }
    public WorkflowActivityTypeId WorkflowActivityTypeId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public Guid? AssignedPersonId { get; private set; }
    public Guid? AssignedGroupId { get; private set; }
    public DateTimeOffset ActivatedAtUtc { get; private set; }
    public DateTimeOffset? LastProcessedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public bool IsCompleted => CompletedAtUtc.HasValue;

    public IReadOnlyList<WorkflowAction> Actions => _actions;

    public IReadOnlyDictionary<string, string?> Attributes
    {
        get => _attributes;
        private set =>
            _attributes = new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

    internal static WorkflowActivity Activate(
        WorkflowId workflowId,
        WorkflowActivityType definition,
        DateTimeOffset utcNow
    )
    {
        var activity = new WorkflowActivity(WorkflowActivityId.Create())
        {
            WorkflowId = workflowId,
            WorkflowActivityTypeId = definition.Id,
            Key = definition.Key,
            Name = definition.Name,
            Order = definition.Order,
            AssignedPersonId = definition.DefaultAssignedPersonId,
            AssignedGroupId = definition.DefaultAssignedGroupId,
            ActivatedAtUtc = utcNow.ToUniversalTime(),
        };

        foreach (var actionDef in definition.Actions.OrderBy(static a => a.Order))
        {
            activity._actions.Add(WorkflowAction.Instantiate(activity.Id, actionDef));
        }

        return activity;
    }

    internal void Assign(Guid? personId, Guid? groupId)
    {
        AssignedPersonId = personId;
        AssignedGroupId = groupId;
    }

    internal void MergeAttributes(IReadOnlyDictionary<string, string?> updates)
    {
        foreach (var (key, value) in updates)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _attributes[key.Trim()] = value;
            }
        }
    }

    internal void MarkProcessed(DateTimeOffset utcNow)
    {
        LastProcessedAtUtc = utcNow.ToUniversalTime();
    }

    internal void MarkCompleted(DateTimeOffset utcNow)
    {
        var normalized = utcNow.ToUniversalTime();
        LastProcessedAtUtc = normalized;
        CompletedAtUtc ??= normalized;
    }

    internal bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (AssignedPersonId != retiredPersonId)
        {
            return false;
        }

        AssignedPersonId = survivingPersonId;
        return true;
    }
}
