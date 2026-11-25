using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Tracks an in-flight PKCE/state/nonce external OIDC authentication session.</summary>
public sealed class RemoteAuthenticationSession : Entity<RemoteAuthenticationSessionId>
{
    private RemoteAuthenticationSession() { }

    private RemoteAuthenticationSession(
        RemoteAuthenticationSessionId id,
        string providerKey,
        string state,
        string nonce,
        string codeVerifier,
        string? returnUrl,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc
    )
        : base(id)
    {
        ProviderKey = providerKey;
        State = state;
        Nonce = nonce;
        CodeVerifier = codeVerifier;
        ReturnUrl = returnUrl;
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
        ExpiresOnUtc = expiresOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the provider key.</summary>
    public string ProviderKey { get; private set; } = string.Empty;

    /// <summary>Gets the anti-CSRF opaque state parameter.</summary>
    public string State { get; private set; } = string.Empty;

    /// <summary>Gets the OIDC ID-token replay-protection nonce.</summary>
    public string Nonce { get; private set; } = string.Empty;

    /// <summary>Gets the PKCE code verifier.</summary>
    public string CodeVerifier { get; private set; } = string.Empty;

    /// <summary>Gets the optional post-login relative return URL.</summary>
    public string? ReturnUrl { get; private set; }

    /// <summary>Gets the UTC creation timestamp.</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>Gets the UTC expiration timestamp.</summary>
    public DateTimeOffset ExpiresOnUtc { get; private set; }

    /// <summary>Gets the UTC completion timestamp once redeemed.</summary>
    public DateTimeOffset? CompletedOnUtc { get; private set; }

    /// <summary>Creates a new remote authentication session.</summary>
    public static RemoteAuthenticationSession Create(
        string providerKey,
        string state,
        string nonce,
        string codeVerifier,
        string? returnUrl,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc
    )
    {
        return new RemoteAuthenticationSession(
            RemoteAuthenticationSessionId.Create(),
            providerKey.Trim().ToLowerInvariant(),
            state,
            nonce,
            codeVerifier,
            returnUrl,
            createdOnUtc,
            expiresOnUtc
        );
    }

    /// <summary>Determines whether the session is valid and uncompleted at <paramref name="utcNow" />.</summary>
    public bool IsValidAt(DateTimeOffset utcNow)
    {
        return !CompletedOnUtc.HasValue && ExpiresOnUtc > utcNow.ToUniversalTime();
    }

    /// <summary>Marks this remote authentication session as completed.</summary>
    public void Complete(DateTimeOffset utcNow)
    {
        CompletedOnUtc = utcNow.ToUniversalTime();
    }
}
