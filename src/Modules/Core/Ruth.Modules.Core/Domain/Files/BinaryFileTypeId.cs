using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="BinaryFileType" />.</summary>
public readonly record struct BinaryFileTypeId(Guid Value) : IStronglyTypedId<BinaryFileTypeId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static BinaryFileTypeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 binary file type identifier.</summary>
    public static BinaryFileTypeId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static BinaryFileTypeId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
