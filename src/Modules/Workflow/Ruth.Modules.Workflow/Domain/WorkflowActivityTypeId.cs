using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowActivityType.</summary>
public readonly record struct WorkflowActivityTypeId(Guid Value)
    : IStronglyTypedId<WorkflowActivityTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowActivityTypeId Empty => new(Guid.Empty);

    public static WorkflowActivityTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowActivityTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
