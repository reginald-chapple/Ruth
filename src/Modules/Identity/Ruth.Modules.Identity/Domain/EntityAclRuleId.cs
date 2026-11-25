using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="EntityAclRule" />.</summary>
public readonly record struct EntityAclRuleId(Guid Value) : IStronglyTypedId<EntityAclRuleId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static EntityAclRuleId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 ACL rule identifier.</summary>
    public static EntityAclRuleId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static EntityAclRuleId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
