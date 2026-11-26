using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain.Definitions;

/// <summary>Ordered action definition owned by a WorkflowActivityType.</summary>
public sealed class WorkflowActionType : Entity<WorkflowActionTypeId>
{
    private Dictionary<string, string?> _configuration = new(StringComparer.OrdinalIgnoreCase);

    private WorkflowActionType() { }

    private WorkflowActionType(WorkflowActionTypeId id)
        : base(id) { }

    public WorkflowActivityTypeId WorkflowActivityTypeId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ActionPluginKey { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public bool IsActivityCompletedOnSuccess { get; private set; }
    public bool IsWorkflowCompletedOnSuccess { get; private set; }

    public IReadOnlyDictionary<string, string?> Configuration
    {
        get => _configuration;
        private set =>
            _configuration = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

    internal static Result<WorkflowActionType> Create(
        WorkflowActivityTypeId activityTypeId,
        WorkflowActionTypeSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new WorkflowActionType(WorkflowActionTypeId.Create())
        {
            WorkflowActivityTypeId = activityTypeId,
        };
        var res = entity.Apply(spec);
        return res.IsFailure
            ? Result.Failure<WorkflowActionType>(res.PrimaryError)
            : Result.Success(entity);
    }

    internal Result Apply(WorkflowActionTypeSpec spec)
    {
        if (
            !WorkflowTextNormalizer.TryNormalizeSlug(spec.Key, out var key)
            || string.IsNullOrWhiteSpace(spec.Name)
            || string.IsNullOrWhiteSpace(spec.ActionPluginKey)
            || spec.Order < 0
        )
        {
            return Result.Failure(WorkflowErrors.WorkflowTypeInvalid);
        }

        Key = key;
        Name = spec.Name.Trim();
        ActionPluginKey = spec.ActionPluginKey.Trim();
        Order = spec.Order;
        IsActivityCompletedOnSuccess = spec.IsActivityCompletedOnSuccess;
        IsWorkflowCompletedOnSuccess = spec.IsWorkflowCompletedOnSuccess;
        _configuration = spec.Configuration is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(spec.Configuration, StringComparer.OrdinalIgnoreCase);

        return Result.Success();
    }
}
