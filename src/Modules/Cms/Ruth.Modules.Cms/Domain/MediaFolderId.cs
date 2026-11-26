using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MediaFolder.</summary>
public readonly record struct MediaFolderId(Guid Value) : IStronglyTypedId<MediaFolderId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MediaFolderId Empty => new(Guid.Empty);

    public static MediaFolderId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MediaFolderId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
