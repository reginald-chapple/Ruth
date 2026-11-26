using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MobileApplication.</summary>
public readonly record struct MobileApplicationId(Guid Value)
    : IStronglyTypedId<MobileApplicationId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MobileApplicationId Empty => new(Guid.Empty);

    public static MobileApplicationId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MobileApplicationId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
