using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PageShortLink.</summary>
public readonly record struct PageShortLinkId(Guid Value) : IStronglyTypedId<PageShortLinkId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PageShortLinkId Empty => new(Guid.Empty);

    public static PageShortLinkId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PageShortLinkId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
