using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="DefinedType" />.</summary>
public readonly record struct DefinedTypeId(Guid Value) : IStronglyTypedId<DefinedTypeId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static DefinedTypeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 defined type identifier.</summary>
    public static DefinedTypeId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static DefinedTypeId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
