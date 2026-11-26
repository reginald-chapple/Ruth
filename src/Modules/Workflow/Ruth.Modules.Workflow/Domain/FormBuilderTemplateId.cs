using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Workflow.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a FormBuilderTemplate.</summary>
public readonly record struct FormBuilderTemplateId(Guid Value)
    : IStronglyTypedId<FormBuilderTemplateId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static FormBuilderTemplateId Empty => new(Guid.Empty);

    public static FormBuilderTemplateId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static FormBuilderTemplateId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
