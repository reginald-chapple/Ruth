using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PipelineActivityType.</summary>
public readonly record struct PipelineActivityTypeId(Guid Value)
    : IStronglyTypedId<PipelineActivityTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PipelineActivityTypeId Empty => new(Guid.Empty);

    public static PipelineActivityTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PipelineActivityTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
