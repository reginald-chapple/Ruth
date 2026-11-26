using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PersonalLinkSection.</summary>
public readonly record struct PersonalLinkSectionId(Guid Value)
    : IStronglyTypedId<PersonalLinkSectionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PersonalLinkSectionId Empty => new(Guid.Empty);

    public static PersonalLinkSectionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PersonalLinkSectionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
