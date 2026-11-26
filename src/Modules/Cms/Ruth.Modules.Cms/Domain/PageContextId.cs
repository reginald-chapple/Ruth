using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PageContext.</summary>
public readonly record struct PageContextId(Guid Value) : IStronglyTypedId<PageContextId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PageContextId Empty => new(Guid.Empty);

    public static PageContextId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PageContextId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
