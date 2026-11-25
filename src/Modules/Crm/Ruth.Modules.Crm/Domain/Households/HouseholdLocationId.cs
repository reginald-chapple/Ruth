using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Households;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="HouseholdLocation" /> entity.</summary>
public readonly record struct HouseholdLocationId(Guid Value)
    : IStronglyTypedId<HouseholdLocationId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static HouseholdLocationId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 household location identifier.</summary>
    public static HouseholdLocationId Create()
    {
        return new HouseholdLocationId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static HouseholdLocationId From(Guid value)
    {
        return new HouseholdLocationId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
