using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Requirements;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupRequirementType" />.</summary>
public readonly record struct GroupRequirementTypeId(Guid Value)
    : IStronglyTypedId<GroupRequirementTypeId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupRequirementTypeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupRequirementTypeId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupRequirementTypeId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
