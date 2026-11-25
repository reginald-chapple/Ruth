using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.GroupTypes;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupTypeRole" /> entity.</summary>
public readonly record struct GroupTypeRoleId(Guid Value) : IStronglyTypedId<GroupTypeRoleId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupTypeRoleId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 GroupTypeRole identifier.</summary>
    public static GroupTypeRoleId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupTypeRoleId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
