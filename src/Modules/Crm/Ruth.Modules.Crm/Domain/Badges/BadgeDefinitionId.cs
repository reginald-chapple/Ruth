using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Badges;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="BadgeDefinition" /> aggregate.</summary>
public readonly record struct BadgeDefinitionId(Guid Value) : IStronglyTypedId<BadgeDefinitionId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static BadgeDefinitionId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 badge definition identifier.</summary>
    public static BadgeDefinitionId Create()
    {
        return new BadgeDefinitionId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static BadgeDefinitionId From(Guid value)
    {
        return new BadgeDefinitionId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
