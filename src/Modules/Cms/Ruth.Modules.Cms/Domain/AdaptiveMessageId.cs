using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an AdaptiveMessage.</summary>
public readonly record struct AdaptiveMessageId(Guid Value) : IStronglyTypedId<AdaptiveMessageId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static AdaptiveMessageId Empty => new(Guid.Empty);

    public static AdaptiveMessageId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static AdaptiveMessageId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
