using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowType.</summary>
public readonly record struct WorkflowTypeId(Guid Value) : IStronglyTypedId<WorkflowTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowTypeId Empty => new(Guid.Empty);

    public static WorkflowTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
