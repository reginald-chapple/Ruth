using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Households;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="HouseholdMember" /> entity.</summary>
public readonly record struct HouseholdMemberId(Guid Value) : IStronglyTypedId<HouseholdMemberId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static HouseholdMemberId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 household member identifier.</summary>
    public static HouseholdMemberId Create()
    {
        return new HouseholdMemberId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static HouseholdMemberId From(Guid value)
    {
        return new HouseholdMemberId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
