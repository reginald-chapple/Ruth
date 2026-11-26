using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowActivity.</summary>
public readonly record struct WorkflowActivityId(Guid Value) : IStronglyTypedId<WorkflowActivityId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowActivityId Empty => new(Guid.Empty);

    public static WorkflowActivityId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowActivityId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
