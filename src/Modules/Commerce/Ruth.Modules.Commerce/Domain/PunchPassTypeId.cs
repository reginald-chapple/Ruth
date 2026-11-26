using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PunchPassType.</summary>
public readonly record struct PunchPassTypeId(Guid Value) : IStronglyTypedId<PunchPassTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PunchPassTypeId Empty => new(Guid.Empty);

    public static PunchPassTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PunchPassTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
