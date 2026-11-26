using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.TwoWaySms;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SmsPipeline" />.</summary>
public readonly record struct SmsPipelineId(Guid Value) : IStronglyTypedId<SmsPipelineId>
{
    public static SmsPipelineId Empty => new(Guid.Empty);

    public static SmsPipelineId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static SmsPipelineId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
