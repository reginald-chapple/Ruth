using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Members;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupMember" /> aggregate.</summary>
public readonly record struct GroupMemberId(Guid Value) : IStronglyTypedId<GroupMemberId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupMemberId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupMemberId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupMemberId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
