using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MergeTemplate.</summary>
public readonly record struct MergeTemplateId(Guid Value) : IStronglyTypedId<MergeTemplateId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MergeTemplateId Empty => new(Guid.Empty);

    public static MergeTemplateId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MergeTemplateId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
