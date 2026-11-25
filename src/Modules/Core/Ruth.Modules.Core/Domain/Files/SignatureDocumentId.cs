using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SignatureDocument" />.</summary>
public readonly record struct SignatureDocumentId(Guid Value)
    : IStronglyTypedId<SignatureDocumentId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static SignatureDocumentId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 signature document identifier.</summary>
    public static SignatureDocumentId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static SignatureDocumentId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
