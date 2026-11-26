using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ServiceOrResourceBooking.</summary>
public readonly record struct ServiceBookingId(Guid Value) : IStronglyTypedId<ServiceBookingId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ServiceBookingId Empty => new(Guid.Empty);

    public static ServiceBookingId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ServiceBookingId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
