using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowLog.</summary>
public readonly record struct WorkflowLogId(Guid Value) : IStronglyTypedId<WorkflowLogId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowLogId Empty => new(Guid.Empty);

    public static WorkflowLogId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowLogId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
