using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MediaAccount.</summary>
public readonly record struct MediaAccountId(Guid Value) : IStronglyTypedId<MediaAccountId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MediaAccountId Empty => new(Guid.Empty);

    public static MediaAccountId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MediaAccountId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
