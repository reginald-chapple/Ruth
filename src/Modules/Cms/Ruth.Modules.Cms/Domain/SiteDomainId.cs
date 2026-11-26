using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a SiteDomain.</summary>
public readonly record struct SiteDomainId(Guid Value) : IStronglyTypedId<SiteDomainId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static SiteDomainId Empty => new(Guid.Empty);

    public static SiteDomainId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static SiteDomainId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
