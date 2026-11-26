using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a CMS Block.</summary>
public readonly record struct CmsBlockId(Guid Value) : IStronglyTypedId<CmsBlockId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CmsBlockId Empty => new(Guid.Empty);

    public static CmsBlockId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static CmsBlockId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
