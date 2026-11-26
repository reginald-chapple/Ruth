using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MobilePage.</summary>
public readonly record struct MobilePageId(Guid Value) : IStronglyTypedId<MobilePageId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MobilePageId Empty => new(Guid.Empty);

    public static MobilePageId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MobilePageId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
