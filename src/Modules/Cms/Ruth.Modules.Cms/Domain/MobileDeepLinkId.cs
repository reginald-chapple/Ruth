using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MobileDeepLink.</summary>
public readonly record struct MobileDeepLinkId(Guid Value) : IStronglyTypedId<MobileDeepLinkId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MobileDeepLinkId Empty => new(Guid.Empty);

    public static MobileDeepLinkId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MobileDeepLinkId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
