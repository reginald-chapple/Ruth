using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Sync;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupSyncRule" />.</summary>
public readonly record struct GroupSyncRuleId(Guid Value) : IStronglyTypedId<GroupSyncRuleId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupSyncRuleId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupSyncRuleId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupSyncRuleId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
