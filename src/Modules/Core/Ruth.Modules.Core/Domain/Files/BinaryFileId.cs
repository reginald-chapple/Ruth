using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="BinaryFile" />.</summary>
public readonly record struct BinaryFileId(Guid Value) : IStronglyTypedId<BinaryFileId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static BinaryFileId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 binary file identifier.</summary>
    public static BinaryFileId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static BinaryFileId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
