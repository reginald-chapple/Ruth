using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="DocumentType" />.</summary>
public readonly record struct DocumentTypeId(Guid Value) : IStronglyTypedId<DocumentTypeId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static DocumentTypeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 document type identifier.</summary>
    public static DocumentTypeId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static DocumentTypeId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
