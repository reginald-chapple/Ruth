using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PipelineType.</summary>
public readonly record struct PipelineTypeId(Guid Value) : IStronglyTypedId<PipelineTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PipelineTypeId Empty => new(Guid.Empty);

    public static PipelineTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PipelineTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
