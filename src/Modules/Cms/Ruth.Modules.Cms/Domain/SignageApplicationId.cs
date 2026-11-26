using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a SignageApplication.</summary>
public readonly record struct SignageApplicationId(Guid Value)
    : IStronglyTypedId<SignageApplicationId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static SignageApplicationId Empty => new(Guid.Empty);

    public static SignageApplicationId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static SignageApplicationId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
