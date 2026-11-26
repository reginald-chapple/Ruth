using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a CMS Theme.</summary>
public readonly record struct CmsThemeId(Guid Value) : IStronglyTypedId<CmsThemeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CmsThemeId Empty => new(Guid.Empty);

    public static CmsThemeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static CmsThemeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
