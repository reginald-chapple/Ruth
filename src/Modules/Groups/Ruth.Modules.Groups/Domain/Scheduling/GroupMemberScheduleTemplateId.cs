using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Scheduling;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupMemberScheduleTemplate" />.</summary>
public readonly record struct GroupMemberScheduleTemplateId(Guid Value)
    : IStronglyTypedId<GroupMemberScheduleTemplateId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupMemberScheduleTemplateId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupMemberScheduleTemplateId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupMemberScheduleTemplateId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
