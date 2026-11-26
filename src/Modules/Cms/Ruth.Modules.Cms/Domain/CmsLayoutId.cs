using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a CMS Layout.</summary>
public readonly record struct CmsLayoutId(Guid Value) : IStronglyTypedId<CmsLayoutId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CmsLayoutId Empty => new(Guid.Empty);

    public static CmsLayoutId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static CmsLayoutId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
