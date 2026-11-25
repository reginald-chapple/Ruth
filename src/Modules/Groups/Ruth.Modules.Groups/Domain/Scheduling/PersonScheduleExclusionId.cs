using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PersonScheduleExclusion" />.</summary>
public readonly record struct PersonScheduleExclusionId(Guid Value)
    : IStronglyTypedId<PersonScheduleExclusionId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PersonScheduleExclusionId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static PersonScheduleExclusionId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PersonScheduleExclusionId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
