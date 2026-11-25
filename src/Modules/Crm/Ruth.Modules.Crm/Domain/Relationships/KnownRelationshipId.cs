using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Relationships;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="KnownRelationship" /> aggregate.</summary>
public readonly record struct KnownRelationshipId(Guid Value)
    : IStronglyTypedId<KnownRelationshipId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static KnownRelationshipId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 known relationship identifier.</summary>
    public static KnownRelationshipId Create()
    {
        return new KnownRelationshipId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static KnownRelationshipId From(Guid value)
    {
        return new KnownRelationshipId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
