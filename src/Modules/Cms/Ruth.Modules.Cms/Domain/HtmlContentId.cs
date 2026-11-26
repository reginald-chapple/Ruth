using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a versioned HtmlContent revision.</summary>
public readonly record struct HtmlContentId(Guid Value) : IStronglyTypedId<HtmlContentId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static HtmlContentId Empty => new(Guid.Empty);

    public static HtmlContentId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static HtmlContentId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
