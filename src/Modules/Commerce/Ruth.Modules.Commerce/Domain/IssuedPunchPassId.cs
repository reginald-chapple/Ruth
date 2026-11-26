using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an IssuedPunchPass.</summary>
public readonly record struct IssuedPunchPassId(Guid Value) : IStronglyTypedId<IssuedPunchPassId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static IssuedPunchPassId Empty => new(Guid.Empty);

    public static IssuedPunchPassId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static IssuedPunchPassId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
