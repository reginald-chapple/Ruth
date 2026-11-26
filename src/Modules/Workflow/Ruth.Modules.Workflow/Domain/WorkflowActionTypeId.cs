using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowActionType.</summary>
public readonly record struct WorkflowActionTypeId(Guid Value)
    : IStronglyTypedId<WorkflowActionTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowActionTypeId Empty => new(Guid.Empty);

    public static WorkflowActionTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowActionTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
