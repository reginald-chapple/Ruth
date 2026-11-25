using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="EntityTypeDescriptor" />.</summary>
public readonly record struct EntityTypeDescriptorId(Guid Value)
    : IStronglyTypedId<EntityTypeDescriptorId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static EntityTypeDescriptorId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 entity type identifier.</summary>
    public static EntityTypeDescriptorId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static EntityTypeDescriptorId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
