using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PersonalLink.</summary>
public readonly record struct PersonalLinkId(Guid Value) : IStronglyTypedId<PersonalLinkId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PersonalLinkId Empty => new(Guid.Empty);

    public static PersonalLinkId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PersonalLinkId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
