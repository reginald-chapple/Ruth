using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a CMS BlockType.</summary>
public readonly record struct CmsBlockTypeId(Guid Value) : IStronglyTypedId<CmsBlockTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CmsBlockTypeId Empty => new(Guid.Empty);

    public static CmsBlockTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static CmsBlockTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
