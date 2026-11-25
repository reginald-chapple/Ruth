using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Households;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="Household" /> aggregate.</summary>
public readonly record struct HouseholdId(Guid Value) : IStronglyTypedId<HouseholdId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static HouseholdId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 household identifier.</summary>
    public static HouseholdId Create()
    {
        return new HouseholdId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static HouseholdId From(Guid value)
    {
        return new HouseholdId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
