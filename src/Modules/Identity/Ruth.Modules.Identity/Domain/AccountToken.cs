using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Single-use, time-bounded token for password reset or account confirmation.</summary>
public sealed class AccountToken : Entity<AccountTokenId>
{
    private AccountToken() { }

    private AccountToken(
        AccountTokenId id,
        UserAccountId userAccountId,
        AccountTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc
    )
        : base(id)
    {
        UserAccountId = userAccountId;
        Purpose = purpose;
        TokenHash = tokenHash;
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
        ExpiresOnUtc = expiresOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the target user account identifier.</summary>
    public UserAccountId UserAccountId { get; private set; }

    /// <summary>Gets the token purpose.</summary>
    public AccountTokenPurpose Purpose { get; private set; }

    /// <summary>Gets the SHA-256 hex digest of the opaque token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gets the UTC creation timestamp.</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>Gets the UTC expiration timestamp.</summary>
    public DateTimeOffset ExpiresOnUtc { get; private set; }

    /// <summary>Gets the UTC timestamp when the token was consumed.</summary>
    public DateTimeOffset? ConsumedOnUtc { get; private set; }

    /// <summary>Creates a new single-use account token.</summary>
    public static AccountToken Create(
        UserAccountId userAccountId,
        AccountTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new AccountToken(
            AccountTokenId.Create(),
            userAccountId,
            purpose,
            tokenHash.Trim(),
            createdOnUtc,
            expiresOnUtc
        );
    }

    /// <summary>Determines whether the token can be consumed at <paramref name="utcNow" />.</summary>
    public bool IsValidAt(DateTimeOffset utcNow)
    {
        return !ConsumedOnUtc.HasValue && ExpiresOnUtc > utcNow.ToUniversalTime();
    }

    /// <summary>Marks this token as consumed.</summary>
    public void Consume(DateTimeOffset utcNow)
    {
        ConsumedOnUtc = utcNow.ToUniversalTime();
    }
}
