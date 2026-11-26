using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PipelineStage.</summary>
public readonly record struct PipelineStageId(Guid Value) : IStronglyTypedId<PipelineStageId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PipelineStageId Empty => new(Guid.Empty);

    public static PipelineStageId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PipelineStageId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
