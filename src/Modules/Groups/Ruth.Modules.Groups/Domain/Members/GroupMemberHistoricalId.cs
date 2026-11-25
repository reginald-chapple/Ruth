using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Members;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupMemberHistorical" /> snapshot.</summary>
public readonly record struct GroupMemberHistoricalId(Guid Value)
    : IStronglyTypedId<GroupMemberHistoricalId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupMemberHistoricalId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupMemberHistoricalId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupMemberHistoricalId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
