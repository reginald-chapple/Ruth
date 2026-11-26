using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowInstance.</summary>
public readonly record struct WorkflowId(Guid Value) : IStronglyTypedId<WorkflowId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowId Empty => new(Guid.Empty);

    public static WorkflowId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
