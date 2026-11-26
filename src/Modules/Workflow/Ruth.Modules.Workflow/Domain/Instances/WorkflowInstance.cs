using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain.Instances;

/// <summary>Persisted state-machine aggregate tracking active workflow activities, actions, attributes, and audit logs.</summary>
public sealed partial class WorkflowInstance
{
    private readonly List<WorkflowActivity> _activities = [];
    private readonly List<WorkflowLog> _logs = [];
    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private WorkflowInstance() { }

    private WorkflowInstance(WorkflowId id)
        : base(id) { }

    public WorkflowTypeId WorkflowTypeId { get; private set; }
    public string WorkflowNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public WorkflowInstanceStatus Status { get; private set; } = WorkflowInstanceStatus.Pending;
    public string StateLabel { get; private set; } = "Initiated";
    public Guid? InitiatorPersonId { get; private set; }
    public string? PrimaryEntityTypeKey { get; private set; }
    public Guid? PrimaryEntityId { get; private set; }
    public DateTimeOffset ActivatedAtUtc { get; private set; }
    public DateTimeOffset? LastProcessedAtUtc { get; private set; }
    public DateTimeOffset? NextScheduledProcessAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int ProcessingAttemptCount { get; private set; }

    public bool IsTerminal =>
        Status
            is WorkflowInstanceStatus.Completed
                or WorkflowInstanceStatus.Faulted
                or WorkflowInstanceStatus.Cancelled;

    public IReadOnlyList<WorkflowActivity> Activities => _activities;
    public IReadOnlyList<WorkflowLog> Logs => _logs;

    public IReadOnlyDictionary<string, string?> Attributes
    {
        get => _attributes;
        private set =>
            _attributes = new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

    public static Result<WorkflowInstance> Launch(
        WorkflowType workflowType,
        string? customName,
        Guid? initiatorPersonId,
        string? primaryEntityTypeKey,
        Guid? primaryEntityId,
        IReadOnlyDictionary<string, string?>? initialAttributes,
        DateTimeOffset utcNow
    )
    {
        ArgumentNullException.ThrowIfNull(workflowType);
        if (!workflowType.IsActive)
        {
            return Result.Failure<WorkflowInstance>(WorkflowErrors.WorkflowTypeInactive);
        }

        var normalizedNow = utcNow.ToUniversalTime();
        var number = workflowType.AllocateNextWorkflowNumber();
        var instanceName = WorkflowTextNormalizer.TrimOrNull(customName, 250)
            ?? $"{workflowType.Name} ({number})";

        var instance = new WorkflowInstance(WorkflowId.Create())
        {
            WorkflowTypeId = workflowType.Id,
            WorkflowNumber = number,
            Name = instanceName,
            Status = WorkflowInstanceStatus.Active,
            StateLabel = "Active",
            InitiatorPersonId = initiatorPersonId,
            PrimaryEntityTypeKey = WorkflowTextNormalizer.TrimOrNull(primaryEntityTypeKey, 160),
            PrimaryEntityId = primaryEntityId,
            ActivatedAtUtc = normalizedNow,
            NextScheduledProcessAtUtc = normalizedNow,
        };

        foreach (var attr in workflowType.DeclaredAttributes)
        {
            if (attr.DefaultValue is not null)
            {
                instance._attributes[attr.Key] = attr.DefaultValue;
            }
        }

        if (initialAttributes is not null)
        {
            instance.MergeAttributes(initialAttributes);
        }

        foreach (
            var actDef in workflowType
                .Activities.Where(static a => a.IsActive && a.IsActivatedWithWorkflow)
                .OrderBy(static a => a.Order)
        )
        {
            instance._activities.Add(WorkflowActivity.Activate(instance.Id, actDef, normalizedNow));
        }

        instance.AddLog(
            workflowType.LoggingLevel,
            WorkflowLogKind.Lifecycle,
            $"Launched workflow '{instance.Name}' ({instance.WorkflowNumber}).",
            normalizedNow,
            initiatorPersonId
        );

        return Result.Success(instance);
    }

    public Result<WorkflowActivity> ActivateActivity(
        WorkflowActivityType definition,
        WorkflowLoggingLevel loggingLevel,
        DateTimeOffset utcNow
    )
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (IsTerminal)
        {
            return Result.Failure<WorkflowActivity>(WorkflowErrors.WorkflowAlreadyClosed);
        }

        var existingOpen = _activities.FirstOrDefault(a =>
            !a.IsCompleted && a.WorkflowActivityTypeId == definition.Id
        );
        if (existingOpen is not null)
        {
            return Result.Success(existingOpen);
        }

        var activated = WorkflowActivity.Activate(Id, definition, utcNow);
        _activities.Add(activated);
        Status = WorkflowInstanceStatus.Active;
        AddLog(
            loggingLevel,
            WorkflowLogKind.Activity,
            $"Activated activity '{definition.Name}'.",
            utcNow
        );
        return Result.Success(activated);
    }

    public void MergeAttributes(IReadOnlyDictionary<string, string?> updates)
    {
        foreach (var (key, value) in updates)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _attributes[key.Trim()] = value;
            }
        }
    }

    public void UpdateStateLabel(string? stateLabel)
    {
        var trimmed = WorkflowTextNormalizer.TrimOrNull(stateLabel, 120);
        if (trimmed is not null)
        {
            StateLabel = trimmed;
        }
    }

    public Result AssignActivity(
        WorkflowActivityId activityId,
        Guid? personId,
        Guid? groupId,
        WorkflowLoggingLevel loggingLevel,
        DateTimeOffset utcNow,
        Guid? actorPersonId = null
    )
    {
        if (IsTerminal)
        {
            return Result.Failure(WorkflowErrors.WorkflowAlreadyClosed);
        }

        var activity = _activities.FirstOrDefault(a => a.Id == activityId && !a.IsCompleted);
        if (activity is null)
        {
            return Result.Failure(WorkflowErrors.ActivityNotFound);
        }

        activity.Assign(personId, groupId);
        AddLog(
            loggingLevel,
            WorkflowLogKind.Activity,
            $"Assigned activity '{activity.Name}'.",
            utcNow,
            actorPersonId
        );
        return Result.Success();
    }
}
