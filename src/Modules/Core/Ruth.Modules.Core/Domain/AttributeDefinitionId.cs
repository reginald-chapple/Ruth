using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AttributeDefinition" />.</summary>
public readonly record struct AttributeDefinitionId(Guid Value)
    : IStronglyTypedId<AttributeDefinitionId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static AttributeDefinitionId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 attribute definition identifier.</summary>
    public static AttributeDefinitionId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static AttributeDefinitionId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
