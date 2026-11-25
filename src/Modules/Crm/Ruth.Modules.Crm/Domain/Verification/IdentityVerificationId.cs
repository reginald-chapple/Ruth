using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Verification;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="IdentityVerification" /> record.</summary>
public readonly record struct IdentityVerificationId(Guid Value)
    : IStronglyTypedId<IdentityVerificationId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static IdentityVerificationId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identity verification identifier.</summary>
    public static IdentityVerificationId Create()
    {
        return new IdentityVerificationId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static IdentityVerificationId From(Guid value)
    {
        return new IdentityVerificationId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
