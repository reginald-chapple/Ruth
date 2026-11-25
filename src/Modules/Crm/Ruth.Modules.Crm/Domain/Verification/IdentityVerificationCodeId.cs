using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Verification;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="IdentityVerificationCode" /> pool entry.</summary>
public readonly record struct IdentityVerificationCodeId(Guid Value)
    : IStronglyTypedId<IdentityVerificationCodeId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static IdentityVerificationCodeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 code identifier.</summary>
    public static IdentityVerificationCodeId Create()
    {
        return new IdentityVerificationCodeId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static IdentityVerificationCodeId From(Guid value)
    {
        return new IdentityVerificationCodeId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
