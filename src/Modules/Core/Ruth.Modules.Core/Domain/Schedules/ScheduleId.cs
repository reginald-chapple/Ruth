using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Schedule" />.</summary>
public readonly record struct ScheduleId(Guid Value) : IStronglyTypedId<ScheduleId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static ScheduleId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 schedule identifier.</summary>
    public static ScheduleId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static ScheduleId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
