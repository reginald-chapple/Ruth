using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowAction.</summary>
public readonly record struct WorkflowActionId(Guid Value) : IStronglyTypedId<WorkflowActionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowActionId Empty => new(Guid.Empty);

    public static WorkflowActionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowActionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
