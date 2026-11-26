using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a WorkflowActionFormSection.</summary>
public readonly record struct WorkflowActionFormSectionId(Guid Value)
    : IStronglyTypedId<WorkflowActionFormSectionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static WorkflowActionFormSectionId Empty => new(Guid.Empty);

    public static WorkflowActionFormSectionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static WorkflowActionFormSectionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
