using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MediaElement.</summary>
public readonly record struct MediaElementId(Guid Value) : IStronglyTypedId<MediaElementId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MediaElementId Empty => new(Guid.Empty);

    public static MediaElementId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MediaElementId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
