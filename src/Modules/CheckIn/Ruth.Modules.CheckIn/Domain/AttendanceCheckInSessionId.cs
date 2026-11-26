using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Sessions.AttendanceCheckInSession" />.</summary>
public readonly record struct AttendanceCheckInSessionId(Guid Value)
    : IStronglyTypedId<AttendanceCheckInSessionId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static AttendanceCheckInSessionId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 check-in session identifier.</summary>
    public static AttendanceCheckInSessionId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static AttendanceCheckInSessionId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
