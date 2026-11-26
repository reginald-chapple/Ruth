using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PageRoute.</summary>
public readonly record struct PageRouteId(Guid Value) : IStronglyTypedId<PageRouteId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PageRouteId Empty => new(Guid.Empty);

    public static PageRouteId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PageRouteId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
