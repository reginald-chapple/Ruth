using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain.Instances;

/// <summary>Audit trail entry recorded during WorkflowInstance lifecycle and step transitions.</summary>
public sealed class WorkflowLog : Entity<WorkflowLogId>
{
    private WorkflowLog() { }

    private WorkflowLog(WorkflowLogId id)
        : base(id) { }

    public WorkflowId WorkflowId { get; private set; }
    public WorkflowLogKind EntryKind { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public Guid? ActorPersonId { get; private set; }
    public DateTimeOffset LoggedAtUtc { get; private set; }

    internal static WorkflowLog Create(
        WorkflowId workflowId,
        WorkflowLogKind kind,
        string summary,
        DateTimeOffset utcNow,
        Guid? actorPersonId = null
    )
    {
        var safeSummary = WorkflowTextNormalizer.TrimOrNull(summary, 2000) ?? kind.ToString();
        return new WorkflowLog(WorkflowLogId.Create())
        {
            WorkflowId = workflowId,
            EntryKind = kind,
            Summary = safeSummary,
            ActorPersonId = actorPersonId,
            LoggedAtUtc = utcNow.ToUniversalTime(),
        };
    }

    internal bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ActorPersonId != retiredPersonId)
        {
            return false;
        }

        ActorPersonId = survivingPersonId;
        return true;
    }
}
