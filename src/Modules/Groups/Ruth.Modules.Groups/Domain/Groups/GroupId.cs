using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="Group" /> aggregate.</summary>
public readonly record struct GroupId(Guid Value) : IStronglyTypedId<GroupId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 Group identifier.</summary>
    public static GroupId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
