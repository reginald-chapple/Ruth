using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a CMS Site.</summary>
public readonly record struct CmsSiteId(Guid Value) : IStronglyTypedId<CmsSiteId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CmsSiteId Empty => new(Guid.Empty);

    public static CmsSiteId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static CmsSiteId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
