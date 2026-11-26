using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an AdaptiveMessageAdaptation.</summary>
public readonly record struct AdaptiveMessageAdaptationId(Guid Value)
    : IStronglyTypedId<AdaptiveMessageAdaptationId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static AdaptiveMessageAdaptationId Empty => new(Guid.Empty);

    public static AdaptiveMessageAdaptationId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static AdaptiveMessageAdaptationId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
