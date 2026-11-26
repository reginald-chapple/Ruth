using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MobileLayout.</summary>
public readonly record struct MobileLayoutId(Guid Value) : IStronglyTypedId<MobileLayoutId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MobileLayoutId Empty => new(Guid.Empty);

    public static MobileLayoutId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MobileLayoutId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
