using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Attendances;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="AttendanceOccurrence" /> aggregate.</summary>
public readonly record struct AttendanceOccurrenceId(Guid Value)
    : IStronglyTypedId<AttendanceOccurrenceId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static AttendanceOccurrenceId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static AttendanceOccurrenceId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static AttendanceOccurrenceId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
