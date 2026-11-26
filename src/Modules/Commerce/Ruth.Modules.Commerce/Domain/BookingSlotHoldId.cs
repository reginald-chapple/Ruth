using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a BookingSlotHold.</summary>
public readonly record struct BookingSlotHoldId(Guid Value) : IStronglyTypedId<BookingSlotHoldId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static BookingSlotHoldId Empty => new(Guid.Empty);

    public static BookingSlotHoldId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static BookingSlotHoldId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
