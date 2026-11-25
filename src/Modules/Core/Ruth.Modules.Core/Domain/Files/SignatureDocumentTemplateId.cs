using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SignatureDocumentTemplate" />.</summary>
public readonly record struct SignatureDocumentTemplateId(Guid Value)
    : IStronglyTypedId<SignatureDocumentTemplateId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static SignatureDocumentTemplateId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 signature document template identifier.</summary>
    public static SignatureDocumentTemplateId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static SignatureDocumentTemplateId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
