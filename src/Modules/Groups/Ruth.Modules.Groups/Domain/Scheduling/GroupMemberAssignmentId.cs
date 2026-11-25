using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupMemberAssignment" />.</summary>
public readonly record struct GroupMemberAssignmentId(Guid Value)
    : IStronglyTypedId<GroupMemberAssignmentId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupMemberAssignmentId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupMemberAssignmentId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupMemberAssignmentId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
