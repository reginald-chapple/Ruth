using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowTrigger.</summary>
public readonly record struct WorkflowTriggerId(Guid Value) : IStronglyTypedId<WorkflowTriggerId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowTriggerId Empty => new(Guid.Empty);

    public static WorkflowTriggerId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowTriggerId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
