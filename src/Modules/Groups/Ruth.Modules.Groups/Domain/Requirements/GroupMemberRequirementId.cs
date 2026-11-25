using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Requirements;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupMemberRequirement" />.</summary>
public readonly record struct GroupMemberRequirementId(Guid Value)
    : IStronglyTypedId<GroupMemberRequirementId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupMemberRequirementId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupMemberRequirementId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupMemberRequirementId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
