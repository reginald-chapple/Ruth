using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AttributeMatrixTemplate" />.</summary>
public readonly record struct AttributeMatrixTemplateId(Guid Value)
    : IStronglyTypedId<AttributeMatrixTemplateId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static AttributeMatrixTemplateId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 attribute matrix template identifier.</summary>
    public static AttributeMatrixTemplateId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static AttributeMatrixTemplateId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
