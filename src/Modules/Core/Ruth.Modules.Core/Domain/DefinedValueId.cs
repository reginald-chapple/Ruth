using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="DefinedValue" />.</summary>
public readonly record struct DefinedValueId(Guid Value) : IStronglyTypedId<DefinedValueId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static DefinedValueId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 defined value identifier.</summary>
    public static DefinedValueId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static DefinedValueId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
