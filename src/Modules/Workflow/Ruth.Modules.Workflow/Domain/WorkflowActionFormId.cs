using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowActionForm.</summary>
public readonly record struct WorkflowActionFormId(Guid Value)
    : IStronglyTypedId<WorkflowActionFormId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowActionFormId Empty => new(Guid.Empty);

    public static WorkflowActionFormId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowActionFormId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
