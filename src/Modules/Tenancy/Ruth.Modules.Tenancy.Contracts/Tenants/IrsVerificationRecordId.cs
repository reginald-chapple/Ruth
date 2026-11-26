using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Strongly-typed identifier for an IRS tax-exempt status verification audit record.</summary>
/// <param name="Value">The underlying UUIDv7 value.</param>
public readonly record struct IrsVerificationRecordId(Guid Value)
    : IStronglyTypedId<IrsVerificationRecordId>
{
    /// <summary>Gets the empty <see cref="IrsVerificationRecordId" />.</summary>
    public static IrsVerificationRecordId Empty => new(Guid.Empty);

    /// <summary>Creates a new time-ordered <see cref="IrsVerificationRecordId" />.</summary>
    public static IrsVerificationRecordId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing <see cref="Guid" />.</summary>
    public static IrsVerificationRecordId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
