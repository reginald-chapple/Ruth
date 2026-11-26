using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a CMS Page.</summary>
public readonly record struct CmsPageId(Guid Value) : IStronglyTypedId<CmsPageId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CmsPageId Empty => new(Guid.Empty);

    public static CmsPageId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static CmsPageId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
