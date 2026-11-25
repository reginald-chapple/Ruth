using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Document" />.</summary>
public readonly record struct DocumentId(Guid Value) : IStronglyTypedId<DocumentId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static DocumentId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 document identifier.</summary>
    public static DocumentId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static DocumentId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
