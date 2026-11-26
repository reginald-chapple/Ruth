using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain.Definitions;

/// <summary>Ordered activity stage definition owned by a WorkflowType.</summary>
public sealed class WorkflowActivityType : Entity<WorkflowActivityTypeId>
{
    private readonly List<WorkflowActionType> _actions = [];

    private WorkflowActivityType() { }

    private WorkflowActivityType(WorkflowActivityTypeId id)
        : base(id) { }

    public WorkflowTypeId WorkflowTypeId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }
    public bool IsActivatedWithWorkflow { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? DefaultAssignedPersonId { get; private set; }
    public Guid? DefaultAssignedGroupId { get; private set; }

    public IReadOnlyList<WorkflowActionType> Actions => _actions;

    internal static Result<WorkflowActivityType> Create(
        WorkflowTypeId workflowTypeId,
        WorkflowActivityTypeSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new WorkflowActivityType(WorkflowActivityTypeId.Create())
        {
            WorkflowTypeId = workflowTypeId,
        };
        var res = entity.Apply(spec);
        return res.IsFailure
            ? Result.Failure<WorkflowActivityType>(res.PrimaryError)
            : Result.Success(entity);
    }

    internal Result Apply(WorkflowActivityTypeSpec spec)
    {
        if (
            !WorkflowTextNormalizer.TryNormalizeSlug(spec.Key, out var key)
            || string.IsNullOrWhiteSpace(spec.Name)
            || spec.Order < 0
        )
        {
            return Result.Failure(WorkflowErrors.WorkflowTypeInvalid);
        }

        Key = key;
        Name = spec.Name.Trim();
        Description = WorkflowTextNormalizer.TrimOrNull(spec.Description);
        Order = spec.Order;
        IsActivatedWithWorkflow = spec.IsActivatedWithWorkflow;
        IsActive = spec.IsActive;
        DefaultAssignedPersonId = spec.DefaultAssignedPersonId;
        DefaultAssignedGroupId = spec.DefaultAssignedGroupId;

        return SyncActions(spec.Actions ?? []);
    }

    internal bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (DefaultAssignedPersonId != retiredPersonId)
        {
            return false;
        }

        DefaultAssignedPersonId = survivingPersonId;
        return true;
    }

    private Result SyncActions(IReadOnlyList<WorkflowActionTypeSpec> specs)
    {
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updatedList = new List<WorkflowActionType>(specs.Count);

        foreach (var actionSpec in specs.OrderBy(static s => s.Order))
        {
            if (
                !WorkflowTextNormalizer.TryNormalizeSlug(actionSpec.Key, out var normKey)
                || !seenKeys.Add(normKey)
            )
            {
                return Result.Failure(WorkflowErrors.WorkflowTypeInvalid);
            }

            var existing = _actions.FirstOrDefault(a =>
                string.Equals(a.Key, normKey, StringComparison.OrdinalIgnoreCase)
            );
            if (existing is not null)
            {
                var applyRes = existing.Apply(actionSpec);
                if (applyRes.IsFailure)
                {
                    return applyRes;
                }

                updatedList.Add(existing);
            }
            else
            {
                var created = WorkflowActionType.Create(Id, actionSpec);
                if (created.IsFailure)
                {
                    return Result.Failure(created.PrimaryError);
                }

                updatedList.Add(created.Value);
            }
        }

        _actions.Clear();
        _actions.AddRange(updatedList);
        return Result.Success();
    }
}
