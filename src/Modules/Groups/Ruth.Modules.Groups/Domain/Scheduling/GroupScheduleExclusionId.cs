using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupScheduleExclusion" />.</summary>
public readonly record struct GroupScheduleExclusionId(Guid Value)
    : IStronglyTypedId<GroupScheduleExclusionId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupScheduleExclusionId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupScheduleExclusionId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupScheduleExclusionId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
