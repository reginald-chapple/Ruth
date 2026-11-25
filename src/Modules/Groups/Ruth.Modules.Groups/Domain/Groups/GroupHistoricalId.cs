using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupHistorical" /> snapshot.</summary>
public readonly record struct GroupHistoricalId(Guid Value) : IStronglyTypedId<GroupHistoricalId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupHistoricalId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupHistoricalId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupHistoricalId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
