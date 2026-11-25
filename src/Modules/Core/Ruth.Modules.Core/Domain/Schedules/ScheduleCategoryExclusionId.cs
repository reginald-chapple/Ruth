using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Schedules;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="ScheduleCategoryExclusion" />.</summary>
public readonly record struct ScheduleCategoryExclusionId(Guid Value)
    : IStronglyTypedId<ScheduleCategoryExclusionId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static ScheduleCategoryExclusionId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 schedule category exclusion identifier.</summary>
    public static ScheduleCategoryExclusionId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static ScheduleCategoryExclusionId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
