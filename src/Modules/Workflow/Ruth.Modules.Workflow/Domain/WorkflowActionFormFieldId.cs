using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowActionFormField.</summary>
public readonly record struct WorkflowActionFormFieldId(Guid Value)
    : IStronglyTypedId<WorkflowActionFormFieldId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowActionFormFieldId Empty => new(Guid.Empty);

    public static WorkflowActionFormFieldId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowActionFormFieldId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
