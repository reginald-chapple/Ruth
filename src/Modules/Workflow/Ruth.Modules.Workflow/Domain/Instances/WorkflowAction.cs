using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain.Instances;

/// <summary>Runtime action step instance inside an active WorkflowActivity.</summary>
public sealed class WorkflowAction : Entity<WorkflowActionId>
{
    private WorkflowAction() { }

    private WorkflowAction(WorkflowActionId id)
        : base(id) { }

    public WorkflowActivityId WorkflowActivityId { get; private set; }
    public WorkflowActionTypeId WorkflowActionTypeId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ActionPluginKey { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public string? FormActionSelected { get; private set; }
    public DateTimeOffset? LastProcessedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public bool IsCompleted => CompletedAtUtc.HasValue;

    internal static WorkflowAction Instantiate(
        WorkflowActivityId activityId,
        WorkflowActionType definition
    )
    {
        return new WorkflowAction(WorkflowActionId.Create())
        {
            WorkflowActivityId = activityId,
            WorkflowActionTypeId = definition.Id,
            Key = definition.Key,
            Name = definition.Name,
            ActionPluginKey = definition.ActionPluginKey,
            Order = definition.Order,
        };
    }

    internal void MarkProcessed(DateTimeOffset utcNow)
    {
        LastProcessedAtUtc = utcNow.ToUniversalTime();
    }

    internal void MarkCompleted(DateTimeOffset utcNow, string? formButtonKey = null)
    {
        var normalized = utcNow.ToUniversalTime();
        LastProcessedAtUtc = normalized;
        CompletedAtUtc = normalized;
        if (!string.IsNullOrWhiteSpace(formButtonKey))
        {
            FormActionSelected = formButtonKey.Trim();
        }
    }
}
