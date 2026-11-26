using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Sessions.CheckInAttendanceEntry" />.</summary>
public readonly record struct CheckInAttendanceEntryId(Guid Value)
    : IStronglyTypedId<CheckInAttendanceEntryId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static CheckInAttendanceEntryId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 check-in attendance entry identifier.</summary>
    public static CheckInAttendanceEntryId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static CheckInAttendanceEntryId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
