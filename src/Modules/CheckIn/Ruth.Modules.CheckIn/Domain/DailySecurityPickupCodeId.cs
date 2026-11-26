using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Sessions.DailySecurityPickupCode" />.</summary>
public readonly record struct DailySecurityPickupCodeId(Guid Value)
    : IStronglyTypedId<DailySecurityPickupCodeId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static DailySecurityPickupCodeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 daily security pickup code identifier.</summary>
    public static DailySecurityPickupCodeId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static DailySecurityPickupCodeId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
