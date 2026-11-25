using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Verification;

/// <summary>6-digit verification code entry (never exposed in public read APIs or reporting projections).</summary>
public sealed class IdentityVerificationCode : Entity<IdentityVerificationCodeId>
{
    private IdentityVerificationCode() { }

    private IdentityVerificationCode(
        IdentityVerificationCodeId id,
        string code,
        DateTimeOffset issuedOnUtc
    )
        : base(id)
    {
        Code = code;
        LastIssuedOnUtc = issuedOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the 6-digit verification code.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when this code was last issued.</summary>
    public DateTimeOffset? LastIssuedOnUtc { get; private set; }

    /// <summary>Creates a new verification code entry.</summary>
    public static IdentityVerificationCode Create(string code, DateTimeOffset issuedOnUtc)
    {
        return new IdentityVerificationCode(IdentityVerificationCodeId.Create(), code, issuedOnUtc);
    }

    /// <summary>Marks this code as re-issued at <paramref name="issuedOnUtc" />.</summary>
    public void MarkIssued(DateTimeOffset issuedOnUtc)
    {
        LastIssuedOnUtc = issuedOnUtc.ToUniversalTime();
    }
}
