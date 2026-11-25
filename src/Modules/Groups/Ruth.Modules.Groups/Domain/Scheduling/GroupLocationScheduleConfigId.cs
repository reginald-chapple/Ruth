using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupLocationScheduleConfig" />.</summary>
public readonly record struct GroupLocationScheduleConfigId(Guid Value)
    : IStronglyTypedId<GroupLocationScheduleConfigId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupLocationScheduleConfigId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupLocationScheduleConfigId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupLocationScheduleConfigId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
