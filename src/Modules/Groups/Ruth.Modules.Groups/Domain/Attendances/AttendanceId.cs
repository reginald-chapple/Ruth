using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Attendances;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="Attendance" /> entity.</summary>
public readonly record struct AttendanceId(Guid Value) : IStronglyTypedId<AttendanceId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static AttendanceId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static AttendanceId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static AttendanceId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
