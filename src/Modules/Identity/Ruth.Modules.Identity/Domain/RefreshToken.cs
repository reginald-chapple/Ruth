using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Rotating, revocable API/mobile refresh token bound to a token family for reuse detection.</summary>
public sealed class RefreshToken : Entity<RefreshTokenId>
{
    private RefreshToken() { }

    private RefreshToken(
        RefreshTokenId id,
        UserAccountId userAccountId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc,
        string? createdByIp
    )
        : base(id)
    {
        UserAccountId = userAccountId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
        ExpiresOnUtc = expiresOnUtc.ToUniversalTime();
        CreatedByIp = createdByIp;
    }

    /// <summary>Gets the owning user account identifier.</summary>
    public UserAccountId UserAccountId { get; private set; }

    /// <summary>Gets the SHA-256 hex digest of the opaque refresh token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gets the rotation family identifier used for reuse detection.</summary>
    public Guid FamilyId { get; private set; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>Gets the UTC expiration timestamp.</summary>
    public DateTimeOffset ExpiresOnUtc { get; private set; }

    /// <summary>Gets the optional client IP address at creation time.</summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>Gets the UTC revocation timestamp when revoked.</summary>
    public DateTimeOffset? RevokedOnUtc { get; private set; }

    /// <summary>Gets the replacement token identifier when rotated.</summary>
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    /// <summary>Gets the optional reason the token was revoked.</summary>
    public string? RevocationReason { get; private set; }

    /// <summary>Creates a newly issued refresh token.</summary>
    public static RefreshToken Create(
        UserAccountId userAccountId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc,
        string? createdByIp = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new RefreshToken(
            RefreshTokenId.Create(),
            userAccountId,
            tokenHash.Trim(),
            familyId == Guid.Empty ? Guid.CreateVersion7() : familyId,
            createdOnUtc,
            expiresOnUtc,
            createdByIp
        );
    }

    /// <summary>Gets a value indicating whether the token has been revoked.</summary>
    public bool IsRevoked => RevokedOnUtc.HasValue;

    /// <summary>Determines whether the token is active (neither revoked nor expired) at <paramref name="utcNow" />.</summary>
    public bool IsActiveAt(DateTimeOffset utcNow)
    {
        return !IsRevoked && ExpiresOnUtc > utcNow.ToUniversalTime();
    }

    /// <summary>Revokes this refresh token.</summary>
    public void Revoke(
        DateTimeOffset utcNow,
        string reason,
        RefreshTokenId? replacedByTokenId = null
    )
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedOnUtc = utcNow.ToUniversalTime();
        RevocationReason = reason;
        ReplacedByTokenId = replacedByTokenId;
    }
}
